using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
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
			Repository = new RepositoryCache(new Repository(GlobalStats), GlobalStats);
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
							var rizeni = Repository.GetInsolvencyProceeding(item.SpisovaZnacka) ?? CreateNewInsolvencyProceeding(item.SpisovaZnacka);

							if (!string.IsNullOrEmpty(item.DokumentUrl))
							{
								Repository.SetDocument(new Dokument { Id = item.Id.ToString(), SpisovaZnacka = item.SpisovaZnacka, Url = item.DokumentUrl, DatumVlozeni = item.DatumZalozeniUdalosti, Popis = item.PopisUdalosti });
								GlobalStats.DocumentCount++;
							}

							if (!string.IsNullOrEmpty(item.Poznamka))
							{
								var xdoc = XDocument.Parse(item.Poznamka);

								var datumVyskrtnuti = ParseValue(xdoc, "//datumVyskrtnuti");
								if (!string.IsNullOrEmpty(datumVyskrtnuti))
								{
									rizeni.Vyskrtnuto = DateTime.Parse(datumVyskrtnuti);
								}

								switch (item.TypUdalosti)
								{
									case "1": // zmena osoby
										ProcessPersonChangedEvent(xdoc, rizeni, item.Id);
										break;
									default:
										var state = xdoc.XPathSelectElement("//vec/druhStavRizeni");
										if (state != null && rizeni.Stav != state.Value)
										{
											rizeni.Stav = state.Value;
											GlobalStats.StateChangedCount++;
										}
										break;
								}

								Repository.SetInsolvencyProceeding(rizeni);
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

		private void ProcessPersonChangedEvent(XDocument xdoc, Rizeni rizeni, long eventId)
		{
			try
			{
				GlobalStats.OsobaChangedEvent++;
				var idPuvodce = ParseValue(xdoc, "//idOsobyPuvodce");
				var osobaId = ParseValue(xdoc, "//osoba/idOsoby");
				var key = $"{idPuvodce}-{osobaId}";
				var osoba = Repository.GetPerson(osobaId, idPuvodce) ?? CreateNewPerson(osobaId, idPuvodce);

				if (!string.IsNullOrEmpty(osoba.SpisovaZnacka) && rizeni.SpisovaZnacka != osoba.SpisovaZnacka)
				{
					throw new ArgumentException("Rozdilna spisova znacka pro stejnou osobu => kombinace IdPuvodce a OsobaId neni dostatecne");
				}
				osoba.SpisovaZnacka = rizeni.SpisovaZnacka;
				osoba.Typ = ParseValue(xdoc, "//osoba/druhOsoby");
				osoba.Role = ParseValue(xdoc, "//osoba/druhRoleVRizeni");
				osoba.Nazev = ParseName(xdoc);

				if (new[] { "F", "SPRÁV_INS", "PAT_ZAST", "DAN_PORAD", "U", "SPRÁVCE_KP" }.Contains(osoba.Typ))
				{
					osoba.Rc = ParseValue(xdoc, "//osoba/rc");
					var date = ParseValue(xdoc, "//osoba/datumNarozeni");
					if (!string.IsNullOrEmpty(date))
					{
						osoba.DatumNarozeni = DateTime.Parse(date);
					}
				}
				else if (new[] { "P", "PODNIKATEL", "OST_OVM", "SPR_ORGAN", "POLICIE", "O", "S", "ADVOKÁT", "EXEKUTOR", "ZNAL_TLUM" }.Contains(osoba.Typ))
				{
					osoba.ICO = ParseValue(xdoc, "//osoba/ic");
					var date = ParseValue(xdoc, "//osoba/datumNarozeni");
					if (!string.IsNullOrEmpty(date))
					{
						osoba.DatumNarozeni = DateTime.Parse(date);
					}
				}
				else
				{
					throw new ApplicationException($"Unknown type of Osoba - {osoba.Typ}");
				}

				Repository.SetPerson(osoba);
			}
			catch (Exception e)
			{
				GlobalStats.WriteError(e.Message, eventId);
			}
		}

		private Osoba CreateNewPerson(string osobaId, string idPuvodce)
		{
			GlobalStats.NewOsobaCount++;
			return new Osoba { IdPuvodce = idPuvodce, Id = osobaId };
		}

		private string ParseName(XDocument xdoc)
		{
			return string.Join(" ", new[] {
											ParseValue(xdoc, "//osoba/titulPred"),
											ParseValue(xdoc, "//osoba/jmeno"),
											ParseValue(xdoc, "//osoba/nazevOsoby"),
											ParseValue(xdoc, "//osoba/titulZa"),
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
				Console.WriteLine($"   Data osoby: R{GlobalStats.PersonGet}/W{GlobalStats.PersonSet} (cache R{GlobalStats.PersonCacheGet}/W{GlobalStats.PersonCacheSet})");
				Console.WriteLine($"   Data dokumenty: R{GlobalStats.DocumentGet}/W{GlobalStats.DocumentSet} (cache R{GlobalStats.DocumentCacheGet}/W{GlobalStats.DocumentCacheSet})");
				Console.WriteLine($"   Data rizeni: R{GlobalStats.InsolvencyProceedingGet}/W{GlobalStats.InsolvencyProceedingSet} (cache R{GlobalStats.InsolvencyProceedingCacheGet}/W{GlobalStats.InsolvencyProceedingCacheSet})");
				Console.WriteLine();
				Console.WriteLine($"   Errors (total: {GlobalStats.TotalErrors}):");
				foreach (var error in GlobalStats.Errors)
				{
					Console.WriteLine($"    - {error}");
				}
				Thread.Sleep(1000);
			}
		}

		private string ParseValue(XDocument xdoc, string xpath)
		{
			return xdoc.XPathSelectElement(xpath)?.Value ?? "";
		}
	}
}
