using HtmlAgilityPack;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsolvencniRejstrik
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("HlidacStatu - datova sada Insolvencni rejstrik");
			Console.WriteLine("----------------------------------------------");
			Console.WriteLine();

			if (args.Length < 1)
			{
				Console.WriteLine("Program je nutne spustit s Vasim autorizacnim tokenem jako prvnim parametrem");
				Console.WriteLine();
				Console.WriteLine("Priklad pouziti:");
				Console.WriteLine("   InsolvencniRejstrik.exe apitokenapitokenapitokenapitoken");

				return;
			}

			var count = DownloadDataAsync(args[0], new DateTime(2008, 1, 14), new DateTime(2008, 3, 1)).Result;

			Console.WriteLine("Stahovani dokonceno");
			Console.ReadKey();
		}

		private static async Task<int> DownloadDataAsync(string apiToken, DateTime fromEndOfPeriodDate, DateTime until, bool removeDatasetIfExists = false, bool updateDatasetIfExists = false)
		{
			var client = new HtmlWeb();
			client.OverrideEncoding = Encoding.GetEncoding("Windows-1250");

			var datasetConnector = new DatasetConnector(apiToken);
			var dataset = InsolvencniRejstrikDataset.InsolvencniRejstrik;
			var count = 0;

			var datasetExists = await datasetConnector.DatasetExists(dataset);
			if (datasetExists && removeDatasetIfExists)
			{
				Console.WriteLine("Maze se stary dataset");
				await datasetConnector.DeleteDataset(dataset);
				datasetExists = false;
			}
			if (datasetExists && updateDatasetIfExists)
			{
				Console.WriteLine("Aktualizuje se dataset");
				Console.WriteLine(" > " + await datasetConnector.UpdateDataset(dataset));
			}
			if (!datasetExists)
			{
				Console.WriteLine("Vytvari se novy dataset");
				Console.WriteLine(" > " + await datasetConnector.RegisterDataset(dataset));
			}

			Console.WriteLine("Zacina import dat do datasetu");

			do
			{
				var content = client.Load($"https://isir.justice.cz/isir/ueu/vysledek_lustrace.do?aktualnost=AKTUALNI_I_UKONCENA&spis_znacky_datum={fromEndOfPeriodDate.ToString("dd.MM.yyyy")}&spis_znacky_obdobi=14DNI");
				var table = content.DocumentNode.Descendants("table").Where(t => t.Attributes["class"]?.Value == "vysledekLustrace").Skip(1).Single();

				foreach (var row in table.Descendants("tr"))
				{
					var items = row.Descendants("td").ToArray();
					if (items.Any())
					{
						var rizeni = new Rizeni
						{
							SpisovaZnacka = new SenatniZnacka
							{
								Soud = items[0].InnerText.Trim(),
								SoudniOddeleni = Convert.ToInt32(items[1].InnerText.Trim()),
								RejstrikovaZnacka = items[2].InnerText.Trim(),
								Cislo = Convert.ToInt32(items[3].InnerText.Replace("/", "").Trim()),
								Rocnik = Convert.ToInt32(items[4].InnerText.Trim())
							},
							Soud = items[5].InnerText.Trim(),
							ZahajeniRizeni = DateTime.ParseExact(items[6].InnerText.Trim(), "dd.MM.yyyy - HH:mm", new CultureInfo("cs-CZ")),
							Nazev = items[7].InnerText.Trim(),
							ICO = items[8].InnerText.Trim(),
							Rc = items[9].InnerText.Trim(),
							RcBezLomitka = items[9].InnerText.Replace("/", "").Trim()
						};
						rizeni.Id = items[7].ChildNodes[1].Attributes["href"]?.Value?.Split(';')?.Skip(1)?.FirstOrDefault();
						rizeni.Url = "https://isir.justice.cz/isir/ueu/evidence_upadcu_detail.do?id=" + rizeni.Id;

						// TODO: nacist detaily rizeni

						await datasetConnector.AddItemToDataset(dataset, rizeni);
						count++;
					}
				}

				Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {fromEndOfPeriodDate.ToShortDateString()} - celkove {count} zaznamu");

				fromEndOfPeriodDate = fromEndOfPeriodDate.AddDays(14);
			} while (fromEndOfPeriodDate < until);

			return count;
		}
	}

	public class Rizeni : IDatasetItem
	{
		public string Id { get; set; }
		public SenatniZnacka SpisovaZnacka { get; set; }
		public string Soud { get; set; }
		public DateTime ZahajeniRizeni { get; set; }
		public string Nazev { get; set; }
		public string ICO { get; set; }
		public string Rc { get; set; }
		public string RcBezLomitka { get; set; }
		// from detail
		public string AktualniStav { get; set; }
		public string Adresa { get; set; }
		public DateTime PosledniZmena { get; set; } = DateTime.MinValue;
		public string InsolvencniSpravce { get; set; }
		public string InsolvencniSpravceKancelar { get; set; }
		public string Url { get; set; }
	}

	public class SpisovaZnacka
	{
		public int SoudniOddeleni { get; set; }
		public string RejstrikovaZnacka { get; set; }
		public int Cislo { get; set; }
		public int Rocnik { get; set; }
	}

	public class SenatniZnacka : SpisovaZnacka
	{
		public string Soud { get; set; }
	}
}
