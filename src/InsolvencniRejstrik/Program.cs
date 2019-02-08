﻿using InsolvencniRejstrik.ByEvents;
using InsolvencniRejstrik.FromSearch;
using NDesk.Options;
using HlidacStatu.Api.Dataset.Connector;
using HtmlAgilityPack;
using System;

namespace InsolvencniRejstrik
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("HlidacStatu - datova sada Insolvencni rejstrik");
			Console.WriteLine("----------------------------------------------");
			Console.WriteLine();

			var search = false;
			var apiToken = "";
			var date = new DateTime(2008, 1, 1);
			var events = false;
			var noCache = false;
			var initCache = false;
			var help = false;
			var createIndexes = false;
			var ignoreDocuments = false;
			var onlyDocuments = false;
			var toEventId = -1;

			var options = new OptionSet() {
				{ "create-indexes", "vytvori indexy v elastiku", v => createIndexes = true },
				{ "s|search", "definuje rezim dle vyhledavani", v => search = true },
				{ "apitoken=", "ApiToken pro pristup k datovym sadam (povinny, pouze pro rezim vyhledavani)", v => apiToken = v },
				{ "date=", "datum zahajeni hledani (nepovinny, default 1.1.2008, pouze pro rezim vyhledavani)", (DateTime v) => date = v },
				{ "e|events", "definuje rezim dle udalosti", v => events = true },
				{ "no-cache", "vypina ukladani event do cache a jejich nasledne pouziti", v => noCache = true },
				{ "ignore-documents", "vypina ukladani dokumentu do databaze", v => ignoreDocuments = true },
				{ "only-documents", "do databaze budou ukladany pouze dokumenty", v => onlyDocuments = true },
				{ "to-event-id=", "nastavuje id udalosti, po ktere dojde k ukonceni zpracovani", v => toEventId = Convert.ToInt32(v) },
				{ "init-link-cache", "nacte seznam vsech rizeni a linku na jejich detail a ulozi je do souboru, ktery je pouzit pri naplneni cache linku na detail rizeni", v => initCache = true },
				{ "h|?|help", "zobrazi napovedu", v => help = true },
			};
			options.Parse(args);

			if (help)
			{
				PrintHelp(options);
			}
			else if (createIndexes)
			{
				var es = new ElasticConnector();
				es.GetESClient(Database.Dokument);
				es.GetESClient(Database.Osoba);
				es.GetESClient(Database.Rizeni);
			}
			else if (initCache)
			{
				Console.WriteLine("Spousti se prednacteni cache (vypsany datum znaci stazene obdobi)");
				new IsirClientCache(null, null).PrepareCache(new DateTime(2008, 1, 1));
			}
			else if (search)
			{
				var connector = new SearchConnector(apiToken);
				connector.Handle(date);
			}
			else if (events)
			{
				var connector = new IsirWsConnector(noCache, ignoreDocuments, onlyDocuments, toEventId);
				connector.Handle();
			}
			else
			{
				PrintHelp(options);
			}
		}

		private static void PrintHelp(OptionSet p) {
			Console.WriteLine($"Program lze spustit ve dvou rezimech");
			Console.WriteLine($"");
			Console.WriteLine($" Dle vyhledavani");
			Console.WriteLine($" - plneni datove sady insolvencniho rejstriku na zaklade");
			Console.WriteLine($"   vyhledavani od zadaneho obdobi do aktualniho dne.");
			Console.WriteLine($"");
			Console.WriteLine($" Dle udalosti");
			Console.WriteLine($" - cteni udalosti o zmenach a jejich zapis a aktualizace");
			Console.WriteLine($"   v interni databazi");
			Console.WriteLine($"");
			Console.WriteLine($"");
			Console.WriteLine($"Parametry spusteni:");
			p.WriteOptionDescriptions(Console.Out);
		}
	}
}
