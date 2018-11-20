using InsolvencniRejstrik.ByEvents;
using InsolvencniRejstrik.FromSearch;
using NDesk.Options;
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
			var help = false;

			var options = new OptionSet() {
				{ "s|search", "definuje rezim dle vyhledavani", v => search = true },
				{ "apitoken=", "ApiToken pro pristup k datovym sadam (povinny, pouze pro rezim vyhledavani)", v => apiToken = v },
				{ "date=", "datum zahajeni hledani (nepovinny, default 1.1.2008, pouze pro rezim vyhledavani)", (DateTime v) => date = v },
				{ "e|events", "definuje rezim dle udalosti", v => events = true },
				{ "no-cache", "vypina ukladani event do cache a jejich nasledne pouziti", v => noCache = true },
				{ "h|?|help", "zobrazi napovedu", v => help = true },
			};

			options.Parse(args);

			if (help)
			{
				PrintHelp(options);
			}
			else if (search)
			{
				var connector = new SearchConnector(apiToken);
				connector.Handle(date);

				Console.WriteLine("Stahovani dokonceno");
				Console.ReadKey();
			}
			else if (events)
			{
				var connector = new IsirWsConnector(noCache);
				connector.Handle();

				Console.WriteLine("Stahovani dokonceno");
				Console.ReadKey();
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
