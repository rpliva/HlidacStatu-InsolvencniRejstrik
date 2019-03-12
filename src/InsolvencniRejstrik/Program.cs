﻿using InsolvencniRejstrik.ByEvents;
using InsolvencniRejstrik.FromSearch;
using NDesk.Options;
using System;
using System.IO;
using Newtonsoft.Json;
using InsolvencniRejstrik.Fixes;

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
			var toEventId = -1;
			var toFiles = false;
			var fromFiles = false;
			var only625fix = false;

			var options = new OptionSet() {
				{ "s|search", "definuje rezim dle vyhledavani", v => search = true },
				{ "apitoken=", "ApiToken pro pristup k datovym sadam (povinny, pouze pro rezim vyhledavani)", v => apiToken = v },
				{ "date=", "datum zahajeni hledani (nepovinny, default 1.1.2008, pouze pro rezim vyhledavani)", (DateTime v) => date = v },
				{ "e|events", "definuje rezim dle udalosti", v => events = true },
				{ "no-cache", "vypina ukladani event do cache a jejich nasledne pouziti", v => noCache = true },
				{ "to-event-id=", "nastavuje id udalosti, po ktere dojde k ukonceni zpracovani", v => toEventId = Convert.ToInt32(v) },
				{ "init-link-cache", "nacte seznam vsech rizeni a linku na jejich detail a ulozi je do souboru, ktery je pouzit pri naplneni cache linku na detail rizeni", v => initCache = true },
				{ "to-files", "uklada rizeni do souboru namisto do databaze", v => toFiles = true},
				{ "from-files", "cte data ze souboru a uklada je do databaze", v => fromFiles = true},
				{ "625-fix", "oprava udalosti 625", v => only625fix = true},
				{ "h|?|help", "zobrazi napovedu", v => help = true },
			};
			options.Parse(args);

			if (help)
			{
				PrintHelp(options);
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
				var stats = new Stats();
				IRepository repository = null;
				IEventsRepository eventsRepository = null;

				if (toFiles)
				{
					repository = new FileRepository();
					eventsRepository = new EventsRepository();
				}
				else
				{
					repository = new RepositoryCache(new Repository(stats));
					eventsRepository = new EventsRepository();
				}

				var connector = new IsirWsConnector(noCache, toEventId, stats, repository, eventsRepository);
				connector.Handle();
			}
			else if (fromFiles)
			{
				var stats = new Stats();
				var repository = new Repository(stats);

				foreach (var dir in Directory.EnumerateDirectories("data"))
				{
					Console.WriteLine($"Zpracovava se slozka {dir} ...");
					Console.WriteLine();
					var count = 0;

					foreach (var file in Directory.EnumerateFiles(dir))
					{
						var rizeni = JsonConvert.DeserializeObject<ByEvents.Rizeni>(File.ReadAllText(file));
						repository.SetInsolvencyProceeding(rizeni);
						if (++count % 100 == 0)
						{
							Console.CursorTop = Console.CursorTop - 1;
							Console.WriteLine($"  {count} rizeni ulozeno");
						}
					}

					Console.CursorTop = Console.CursorTop - 1;
					Console.WriteLine($"  {count} rizeni ulozeno");
					Console.WriteLine();
				}
			}
			else if (only625fix)
			{
				new Event625(new ElasticConnector()).Execute();
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
