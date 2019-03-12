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

		public void Execute()
		{
			Console.WriteLine("Oprava zpracovani udalosti typu 625");
			Console.WriteLine();
			Console.WriteLine();

			if (File.Exists(WsClientCache.CacheFile))
			{
				foreach (var item in File.ReadLines(WsClientCache.CacheFile).Select(l => WsResult.From(l)))
				{
					Lines++;
					if (item.TypUdalosti == "625")
					{
						FixingEvents++;
						ProcessEvent(item);
					}

					if (Lines % 10000 == 0)
					{
						Console.CursorTop = Console.CursorTop - 1;
						Console.WriteLine($"Precteno {Lines} radku z toho {FixingEvents} opravovanych udalosti a pridano {Added} veritelu");
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
			//throw new NotImplementedException();
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
