using Elasticsearch.Net;
using InsolvencniRejstrik.ByEvents;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Nest;

namespace InsolvencniRejstrik.Fixes
{
	class Event625
	{
		private readonly ElasticConnector Connector;

		private int Lines = 0;
		private int FixingEvents = 0;
		private int Added = 0;

		public Event625(ElasticConnector connector)
		{
			Connector = connector;
		}

		public void Execute(int skipLines)
		{
			Console.WriteLine("Oprava zpracovani udalosti typu 625");
			if (skipLines > 0)
			{
				Console.WriteLine($"{skipLines} radek bude preskoceno");
			}
			Console.WriteLine();
			Console.WriteLine();

			if (File.Exists(WsClientCache.CacheFile))
			{
				foreach (var line in File.ReadLines(WsClientCache.CacheFile))
				{
					try
					{
						var item = WsResult.From(line);
						Lines++;
						if (Lines < skipLines) continue;

						if (item.TypUdalosti == "625")
						{
							FixingEvents++;
							ProcessEvent(item);
						}

						if (Lines % 10000 == 0)
						{
							Console.CursorTop = Console.CursorTop - 1;
							Console.WriteLine($"Precteno {Lines} radku z toho {FixingEvents} opravovanych udalosti a pridano {Added} veritelu (id udalosti: {item.Id})");
						}
					}
					catch (UnknownPersonException e)
					{
						using (var stream = File.AppendText("unknown-person-type.log"))
						{
							stream.WriteLine(line);
							stream.Flush();
						}
						Console.WriteLine();
						Console.WriteLine($"ERROR: {e.Message}");
						Console.WriteLine();
						Console.WriteLine();
					}
					catch (UnknownRoleException e)
					{
						using (var stream = File.AppendText("unknown-role-type.log"))
						{
							stream.WriteLine(line);
							stream.Flush();
						}
						Console.WriteLine();
						Console.WriteLine($"ERROR: {e.Message}");
						Console.WriteLine();
						Console.WriteLine();
					}
				}
			}
		}

		private void ProcessEvent(WsResult item)
		{
			var osoba = ParseOsoba(XDocument.Parse(item.Poznamka));
			var rizeni = LoadRizeni(item.SpisovaZnacka);
			if (rizeni == null)
			{
				Console.WriteLine($"Rizeni {item.SpisovaZnacka} nebylo nalezeno");
				Console.WriteLine();
				return;
			}

			if (!rizeni.Veritele.Any(o => o.IdOsoby == osoba.IdOsoby && o.IdPuvodce == osoba.IdPuvodce))
			{
				rizeni.Veritele.Add(osoba);
				SaveRizeni(rizeni);
				Added++;
			}
		}

		private Rizeni LoadRizeni(string spisovaZnacka)
		{
			var res = Connector.GetESClient().Get<Rizeni>(spisovaZnacka, s => s.SourceExclude("dokumenty"));
			return res.Found ? res.Source : null;
		}

		private void SaveRizeni(Rizeni rizeni)
		{
			//var esUrl = Connector.GetESClient().ConnectionSettings.ConnectionPool.Nodes.First().Uri.ToString();
			//esUrl += $"insolvencnirestrik/rizeni/{System.Net.WebUtility.UrlEncode(rizeni.SpisovaZnacka)}/_update";
			//try
			//{
			//	using (Devmasters.Net.Web.URLContent url = new Devmasters.Net.Web.URLContent(esUrl.ToString()))
			//	{

			//		url.Method = Devmasters.Net.Web.MethodEnum.POST;
			//		url.Tries = 3;
			//		url.TimeInMsBetweenTries = 500;

			//		var postContent = "{\"doc\" : {\"veritele\" : " +
			//			Newtonsoft.Json.JsonConvert.SerializeObject(rizeni.Veritele)
			//			+ "}}";
			//		url.RequestParams.RawContent = postContent;
			//		var esres = url.GetContent();
			//	}
			//}
			//catch (Devmasters.Net.Web.UrlContentException e)
			//{
			//	Console.WriteLine($"Rizeni {rizeni.SpisovaZnacka} - chyba ukladani");

			//}
			//catch (Exception e)
			//{
			//	Console.WriteLine($"Rizeni {rizeni.SpisovaZnacka} - chyba ukladani " + e.ToString());
			//}
		}

		private Osoba ParseOsoba(XDocument doc)
		{
			var idPuvodce = IsirWsConnector.ParseValue(doc, "idOsobyPuvodce");
			var o = doc.Descendants("osoba").FirstOrDefault();
			var osobaId = IsirWsConnector.ParseValue(o, "idOsoby");
			var role = IsirWsConnector.ParseValue(o, "druhRoleVRizeni");
			var osoba = new Osoba { IdOsoby = osobaId, IdPuvodce = idPuvodce, Role = role };

			IsirWsConnector.UpdatePerson(osoba, doc.Descendants("osoba").FirstOrDefault());

			return osoba;
		}
	}
}
