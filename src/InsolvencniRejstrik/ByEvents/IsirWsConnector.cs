﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace InsolvencniRejstrik.ByEvents
{
	partial class IsirWsConnector : BaseConnector
	{
		private readonly IRepository Repository;
		private readonly EventsRepository EventsRepository;
		private readonly IIsirClient IsirClient;
		private readonly IWsClient WsClient;


		public IsirWsConnector(bool noCache)
		{
			GlobalStats = new Stats();
			Repository = new RepositoryCache(new Repository(GlobalStats), CreateNewInsolvencyProceeding, CreateNewPerson);
			EventsRepository = new EventsRepository();
			IsirClient = noCache ? (IIsirClient)new IsirClient(GlobalStats) : new IsirClientCache(new IsirClient(GlobalStats), GlobalStats);
			WsClient = noCache ? (IWsClient)new WsClient() : new WsClientCache(new Lazy<IWsClient>(() => new WsClient()));
		}

		public void Handle()
		{
			Console.WriteLine("Spousti se zpracovani ...");

			WsProcessorTask = RunTask(() => WsProcessor(EventsRepository.GetLastEventId()));
			LinkProcessorTask = RunTask(LinkProcessor);
			MessageProcessorTask = RunTask(MessageProcessor);
			var StatsInfo = RunTask(StatsInfoCallback);

			while (MessageProcessorTask.Status == TaskStatus.Running || LinkProcessorTask.Status == TaskStatus.Running)
			{
				Thread.Sleep(500);
			}
		}

		private Stats GlobalStats;
		private Task WsProcessorTask;
		private Task LinkProcessorTask;
		private Task MessageProcessorTask;
		private ConcurrentQueue<WsResult> WsResultsQueue = new ConcurrentQueue<WsResult>();
		private ConcurrentQueue<Rizeni> LinkRequestsQueue = new ConcurrentQueue<Rizeni>();

		private void WsProcessor(long id)
		{
			var lastId = id;
			while (true)
			{
				try
				{
					foreach (var item in WsClient.Get(id))
					{
						WsResultsQueue.Enqueue(item);
						lastId = item.Id;

						while (WsResultsQueue.Count > 3000)
						{
							Thread.Sleep(10_000);
						}
					}

					return;
				}
				catch (Exception e)
				{
					GlobalStats.WriteError("WS processor - " + e.Message, lastId);
				}
			}
		}

		private void LinkProcessor()
		{
			while (!LinkRequestsQueue.IsEmpty || WsProcessorTask.Status == TaskStatus.Running || MessageProcessorTask.Status == TaskStatus.Running)
			{
				LinkRequestsQueue.TryDequeue(out var item);
				if (item != null)
				{
					item.Url = IsirClient.GetUrl(item.SpisovaZnacka);
					item.Soud = IsirClient.GetSoud(item.SpisovaZnacka);
				}
				else
				{
					Thread.Sleep(100);
				}
			}
		}

		private void MessageProcessor()
		{
			while (!WsResultsQueue.IsEmpty || WsProcessorTask.Status == TaskStatus.Running)
			{
				WsResult item;
				WsResultsQueue.TryDequeue(out item);
				if (item != null)
				{
					GlobalStats.EventsCount++;
					GlobalStats.LastEventId = item.Id;
					GlobalStats.LastEventTime = item.DatumZalozeniUdalosti;
					var retry = true;

					while (retry)
					{
						try
						{
							var rizeni = Repository.GetInsolvencyProceeding(item.SpisovaZnacka);
							var lastChanged = rizeni.PosledniZmena;

							if (!string.IsNullOrEmpty(item.DokumentUrl))
							{
								Repository.SetDocument(new Dokument { Id = item.Id.ToString(), SpisovaZnacka = item.SpisovaZnacka, Url = item.DokumentUrl, DatumVlozeni = item.DatumZalozeniUdalosti, Popis = item.PopisUdalosti });
								GlobalStats.DocumentCount++;
							}

							if (!string.IsNullOrEmpty(item.Poznamka))
							{
								var xdoc = XDocument.Parse(item.Poznamka);

								var datumVyskrtnuti = ParseValue(xdoc, "datumVyskrtnuti");
								if (!string.IsNullOrEmpty(datumVyskrtnuti))
								{
									rizeni.Vyskrtnuto = DateTime.Parse(datumVyskrtnuti);
									rizeni.PosledniZmena = item.DatumZalozeniUdalosti;
								}

								switch (item.TypUdalosti)
								{
									case "1": // zmena osoby
										var osoba = ProcessPersonChangedEvent(xdoc, rizeni, item.Id);

										var subjekt = new Subjekt { Nazev = osoba.Nazev.ToUpperInvariant(), ICO = osoba.ICO, Rc = osoba.Rc };
										if (!rizeni.Subjekty.Contains(subjekt))
										{
											rizeni.Subjekty.Add(subjekt);
											rizeni.PosledniZmena = item.DatumZalozeniUdalosti;
										}

										break;
									case "2": // zmena adresy osoby
										var osobaSAdresou = ProcessAddressChangedEvent(xdoc, rizeni, item.Id);

										var subjektSAdresou = new Subjekt { Nazev = osobaSAdresou.Nazev.ToUpperInvariant(), ICO = osobaSAdresou.ICO, Rc = osobaSAdresou.Rc };
										if (!rizeni.Subjekty.Contains(subjektSAdresou))
										{
											rizeni.Subjekty.Add(subjektSAdresou);
											rizeni.PosledniZmena = item.DatumZalozeniUdalosti;
										}
										break;
									case "5": // insolvencni navrh
										rizeni.DatumZalozeni = item.DatumZalozeniUdalosti;
										rizeni.PosledniZmena = item.DatumZalozeniUdalosti;
										break;
									default:
										var state = ParseValue(xdoc.Descendants("vec").FirstOrDefault(), "druhStavRizeni");
										if (!string.IsNullOrEmpty(state) && rizeni.Stav != state)
										{
											rizeni.Stav = state;
											rizeni.PosledniZmena = item.DatumZalozeniUdalosti;
											GlobalStats.StateChangedCount++;
										}
										break;
								}

								if (lastChanged != rizeni.PosledniZmena)
								{
									Repository.SetInsolvencyProceeding(rizeni);
								}
							}
							EventsRepository.SetLastEventId(item.Id);
							retry = false;
						}
						catch (Exception e)
						{
							GlobalStats.WriteError($"Message task - {e.Message}", item.Id);
							File.AppendAllText("errors.log", $@"""
[{DateTime.Now}]
{item.Id} - {item.SpisovaZnacka} - {item.PopisUdalosti}
{e.Message}
{e.StackTrace}
""");
							retry = true;
							Thread.Sleep(100);
						}
					}
				}
				else
				{
					Thread.Sleep(100);
				}
			}
		}

		private Rizeni CreateNewInsolvencyProceeding(string spisovaZnacka)
		{
			GlobalStats.RizeniCount++;
			var r = new Rizeni { SpisovaZnacka = spisovaZnacka };
			LinkRequestsQueue.Enqueue(r);
			return r;
		}

		private HashSet<string> FyzickeOsoby = new HashSet<string> { "F", "SPRÁV_INS", "PAT_ZAST", "DAN_PORAD", "U", "SPRÁVCE_KP" };
		private HashSet<string> PravnickeOsoby = new HashSet<string> { "P", "PODNIKATEL", "OST_OVM", "SPR_ORGAN", "POLICIE", "O", "S", "ADVOKÁT", "EXEKUTOR", "ZNAL_TLUM" };

		private Osoba ProcessPersonChangedEvent(XDocument xdoc, Rizeni rizeni, long eventId)
		{
			try
			{
				GlobalStats.OsobaChangedEvent++;
				var idPuvodce = ParseValue(xdoc, "idOsobyPuvodce");
				var o = xdoc.Descendants("osoba").FirstOrDefault();
				var osobaId = ParseValue(o, "idOsoby");
				var key = new OsobaId { IdOsoby = osobaId, IdPuvodce = idPuvodce, SpisovaZnacka = rizeni.SpisovaZnacka };
				var osoba = Repository.GetPerson(key);

				if (UpdatePerson(osoba, o))
				{
					Repository.SetPerson(osoba);
				}

				return osoba;
			}
			catch (Exception e)
			{
				GlobalStats.WriteError(e.Message, eventId);
				throw;
			}
		}

		private bool Update<T,U>(T target, Expression<Func<T, U>> item, U value)
		{
			var expr = (MemberExpression)item.Body;
			var prop = (PropertyInfo)expr.Member;
			if (!(((U)prop.GetValue(target))?.Equals(value) ?? false))
			{
				prop.SetValue(target, value, null);
				return true;
			}
			return false;
		}

		private bool UpdatePerson(Osoba person, XElement element)
		{
			var changed = false;
			changed |= Update(person, p => p.Typ, ParseValue(element, "druhOsoby"));
			changed |= Update(person, p => p.Role, ParseValue(element, "druhRoleVRizeni"));
			changed |= Update(person, p => p.Nazev, ParseName(element));

			if (FyzickeOsoby.Contains(person.Typ))
			{
				changed |= Update(person, p => p.Rc, ParseValue(element, "rc"));
				var date = ParseValue(element, "datumNarozeni");
				if (!string.IsNullOrEmpty(date))
				{
					changed |= Update(person, p => p.DatumNarozeni, DateTime.Parse(date));
				}
			}
			else if (PravnickeOsoby.Contains(person.Typ))
			{
				changed |= Update(person, p => p.ICO, ParseValue(element, "ic"));
				var date = ParseValue(element, "datumNarozeni");
				if (!string.IsNullOrEmpty(date))
				{
					changed |= Update(person, p => p.DatumNarozeni, DateTime.Parse(date));
				}
			}
			else
			{
				throw new ApplicationException($"Unknown type of Osoba - {person.Typ}");
			}

			return changed;
		}

		private Osoba ProcessAddressChangedEvent(XDocument xdoc, Rizeni rizeni, long eventId)
		{
			try
			{
				GlobalStats.AdresaChangedEvent++;
				var idPuvodce = ParseValue(xdoc, "idOsobyPuvodce");
				var o = xdoc.Descendants("osoba").FirstOrDefault();
				var osobaId = ParseValue(o, "idOsoby");
				var key = new OsobaId { IdOsoby = osobaId, IdPuvodce = idPuvodce, SpisovaZnacka = rizeni.SpisovaZnacka };
				var osoba = Repository.GetPerson(key);

				var changed = UpdatePerson(osoba, o);

				var a = o.Descendants("adresa").FirstOrDefault();

				if (a != null)
				{
					var druhAdresy = ParseValue(a, "druhAdresy");
					if (druhAdresy == "TRVALÁ" || druhAdresy == "SÍDLO FY")
					{
						changed |= Update(osoba, p => p.Mesto, ParseValue(a, "mesto"));
						changed |= Update(osoba, p => p.Okres, ParseValue(a, "okres"));
						changed |= Update(osoba, p => p.Zeme, ParseValue(a, "zeme"));
						changed |= Update(osoba, p => p.Psc, ParseValue(a, "psc"));
					}
				}

				if (changed)
				{
					Repository.SetPerson(osoba);
				}

				return osoba;
			}
			catch (Exception e)
			{
				GlobalStats.WriteError(e.Message, eventId);
				throw;
			}
		}

		private Osoba CreateNewPerson(OsobaId id)
		{
			GlobalStats.NewOsobaCount++;
			return new Osoba { IdPuvodce = id.IdPuvodce, IdOsoby = id.IdOsoby, Id = id.GetId(), SpisovaZnacka = id.SpisovaZnacka };
		}

		private string ParseName(XElement o)
		{
			return string.Join(" ", new[] {
											ParseValue(o, "titulPred"),
											ParseValue(o, "jmeno"),
											ParseValue(o, "nazevOsoby"),
											 ParseValue(o, "titulZa"),
										}.Where(i => !string.IsNullOrEmpty(i)));
		}

		private void StatsInfoCallback()
		{
			while (true)
			{
				PrintHeader();
				var speed = GlobalStats.EventsCount / GlobalStats.Duration().TotalSeconds;
				var remains = speed > 0 && GlobalStats.LastEventId < 39_000_000
					? $" => {TimeSpan.FromSeconds((39_000_000 - GlobalStats.EventsCount) / speed)}"
					: string.Empty;
				Console.WriteLine($"   Zpracovano udalosti: {GlobalStats.EventsCount} ({speed:0.00} udalost/s{remains})");
				Console.WriteLine($"   Doba behu: {GlobalStats.RunningTime()}");
				Console.WriteLine();
				Console.WriteLine($"   Nacteno rizeni: {GlobalStats.RizeniCount}");
				Console.WriteLine($"   Nacteno dokumentu: {GlobalStats.DocumentCount}");
				Console.WriteLine($"   Nacteno linku: {GlobalStats.LinkCount} ({GlobalStats.LinkCacheCount})");
				Console.WriteLine();
				Console.WriteLine($"   Fronta zprav: {WsResultsQueue.Count}");
				Console.WriteLine($"   Fronta linku: {LinkRequestsQueue.Count}");
				Console.WriteLine();
				Console.WriteLine($"   Pocet udalosti zmeny osoby: {GlobalStats.OsobaChangedEvent}");
				Console.WriteLine($"   Pocet udalosti zmeny adresy: {GlobalStats.AdresaChangedEvent}");
				Console.WriteLine($"   Pocet novych osob: {GlobalStats.NewOsobaCount}");
				Console.WriteLine($"   Pocet zmen stavu rizeni: {GlobalStats.StateChangedCount}");
				Console.WriteLine();
				Console.WriteLine($"   Posledni zpracovavane id udalosti: {GlobalStats.LastEventId}");
				Console.WriteLine($"   Datum posledni zpravovavane udalosti: {GlobalStats.LastEventTime.ToShortDateString()}");
				Console.WriteLine();
				Console.WriteLine($"   Vlakno WS: {WsProcessorTask.Status}");
				Console.WriteLine($"   Vlakno zprav: {MessageProcessorTask.Status}");
				Console.WriteLine($"   Vlakno linku: {LinkProcessorTask.Status}");
				Console.WriteLine();
				Console.WriteLine($"   Data osoby: R{GlobalStats.PersonGet}/W{GlobalStats.PersonSet}");
				Console.WriteLine($"   Data dokumenty: R{GlobalStats.DocumentGet}/W{GlobalStats.DocumentSet}");
				Console.WriteLine($"   Data rizeni: R{GlobalStats.InsolvencyProceedingGet}/W{GlobalStats.InsolvencyProceedingSet}");
				Console.WriteLine();
				Console.WriteLine($"   Errors (total: {GlobalStats.TotalErrors}):");
				foreach (var error in GlobalStats.Errors.ToArray())
				{
					Console.WriteLine($"    - {error}");
				}
				Thread.Sleep(1000);
			}
		}

		private string ParseValue(XElement xel, string element)
		{
			return xel?.Element(XName.Get(element))?.Value ?? "";
		}

		private string ParseValue(XDocument xdoc, string element)
		{
			return xdoc.Descendants(element).FirstOrDefault()?.Value ?? "";
		}
	}
}
