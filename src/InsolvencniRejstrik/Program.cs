using HlidacStatu.Api.Dataset.Connector;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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
				Console.WriteLine("Program lze take spustit s Vasim autorizacnim tokenem jako prvnim parametrem, nebude pak nutne jej zadavat v programu rucne");
				Console.WriteLine();
				Console.WriteLine("Priklad pouziti:");
				Console.WriteLine("   InsolvencniRejstrik.exe apitokenapitokenapitokenapitoken");
				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine("Zadajte Vas API token: ");
				ApiToken = Console.ReadLine();
			}
			else
			{
				ApiToken = args[0];
			}

			PrepareDataset();

			Console.WriteLine("Pripravuje se prostredi");
			var tf = new TaskFactory();
			TaskSearch = tf.StartNew(() => SearchInsolvencyProceedings(new DateTime(2008, 1, 14), DateTime.Now.AddDays(-1)));
			while (TaskSearch.Status != TaskStatus.Running) Thread.Sleep(10);
			TaskWs = new Task[WsThreads];
			for (int i = 0; i < WsThreads; i++)
			{
				TaskWs[i] = tf.StartNew(() => RequestsToWs());
				while (TaskWs[i].Status != TaskStatus.Running) Thread.Sleep(10);
			}
			TaskStore = new Task[StoreThreads];
			for (int i = 0; i < WsThreads; i++)
			{
				TaskStore[i] = tf.StartNew(() => StoreToHlidacStatu());
				while (TaskStore[i].Status != TaskStatus.Running) Thread.Sleep(10);
			}

			var start = DateTime.Now;

			while (TaskStore.Any(t => t.Status == TaskStatus.Running))
			{
				Console.Clear();
				Console.WriteLine("HlidacStatu - datova sada Insolvencni rejstrik");
				Console.WriteLine("----------------------------------------------");
				Console.WriteLine();
				Console.WriteLine($"   Nacteno z vyhledavani: {ReadFromSearch}");
				Console.WriteLine($"   Ceka na nacteni detailu: {ForDetailRequest.Count}");
				Console.WriteLine($"   Ceka a odeslani do hlidace: {ForStore.Count}");
				Console.WriteLine($"   Celkove ulozeno: {StoredItems}");
				Console.WriteLine();
				Console.WriteLine($"   Odlisna spisova znacka: {InvalidFileNumber}");
				Console.WriteLine($"   Znepristupneno: {Disabled}");
				Console.WriteLine();
				Console.WriteLine($"   Posledni nactene obdobi: {LastFinishedPeriodInSearch: dd.MM.yyyy}");
				Console.WriteLine($"   Posledni ulozeny zaznam: {LastStoredItem: dd.MM.yyyy}");
				Console.WriteLine();
				Console.WriteLine($"   Vyhledavaci vlakno: {TaskSearch.Status}");
				Console.WriteLine($"   Vlakno WS: {string.Join(", ", TaskWs.Select(t => t.Status.ToString()))}");
				Console.WriteLine($"   Vlakno ukladani: {string.Join(", ", TaskStore.Select(t => t.Status.ToString()))}");
				Console.WriteLine();
				var duration = DateTime.Now - start;
				Console.WriteLine($"   Doba behu: {duration.Hours:00}:{duration.Minutes:00}:{duration.Seconds:00}");
				Console.WriteLine();
				try
				{
					if (Errors.Any())
					{
						Console.WriteLine($"   Chyby:");
						foreach (var error in Errors)
						{
							Console.WriteLine($"    > {error}");
						}
					}
				}
				catch (Exception e)
				{
					// do nothing
				}

				Thread.Sleep(1000);
			}


			Console.WriteLine("Stahovani dokonceno");
			Console.ReadKey();
		}

		private static string ApiToken = "";
		private static DateTime LastFinishedPeriodInSearch = DateTime.MinValue;
		private static DateTime LastStoredItem = DateTime.MinValue;
		private static int WsThreads = 4;
		private static int StoreThreads = 4;
		private static int ReadFromSearch = 0;
		private static int InvalidFileNumber = 0;
		private static int StoredItems = 0;
		private static int Disabled = 0;
		private static Task TaskSearch;
		private static Task[] TaskWs;
		private static Task[] TaskStore;
		private static ConcurrentQueue<Rizeni> ForDetailRequest = new ConcurrentQueue<Rizeni>();
		private static ConcurrentQueue<Rizeni> ForStore = new ConcurrentQueue<Rizeni>();
		private static List<string> Errors = new List<string>();

		private static HtmlNode MakeSearchRequest(HtmlWeb client, DateTime fromEndOfPeriodDate)
		{
			var content = client.Load($"https://isir.justice.cz/isir/ueu/vysledek_lustrace.do?aktualnost=AKTUALNI_I_UKONCENA&spis_znacky_datum={fromEndOfPeriodDate.ToString("dd.MM.yyyy")}&spis_znacky_obdobi=14DNI");
			return content.DocumentNode.Descendants("table").Where(t => t.Attributes["class"]?.Value == "vysledekLustrace").Skip(1).Single();
		}

		private static void SearchInsolvencyProceedings(DateTime fromEndOfPeriodDate, DateTime until)
		{
			var client = new HtmlWeb();
			client.OverrideEncoding = Encoding.GetEncoding("Windows-1250");

			do
			{
				HtmlNode table;

				// slow down if queue of requests to WS is too high
				while (ForDetailRequest.Count > 500)
				{
					Thread.Sleep(5000);
				}

				// retrying until get correct answer :)
				while (true)
				{
					try
					{
						table = MakeSearchRequest(client, fromEndOfPeriodDate);
						break;
					}
					catch (Exception e)
					{
						AddError("Search", e);
					}
				}

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
								SoudniOddeleni = string.IsNullOrEmpty(items[1].InnerText.Trim()) ? 0 : Convert.ToInt32(items[1].InnerText.Trim()),
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
						rizeni.AktualniStav = "ZNEPRISTUPNENO";

						ForDetailRequest.Enqueue(rizeni);
						Interlocked.Increment(ref ReadFromSearch);
					}
				}

				LastFinishedPeriodInSearch = fromEndOfPeriodDate;
				fromEndOfPeriodDate = fromEndOfPeriodDate.AddDays(14);
			} while (fromEndOfPeriodDate < until);

			Thread.Sleep(1000);
		}

		private static Isir.getIsirWsCuzkDataResponse MakeWsRequest(Isir.IsirWsCuzkPortTypeClient wsClient, string ico, string rc)
		{
			return wsClient.getIsirWsCuzkDataAsync(string.IsNullOrEmpty(ico)
						? new Isir.getIsirWsCuzkDataRequest { rc = rc, filtrAktualniRizeni = Isir.priznakType.F, maxPocetVysledku = 50, maxRelevanceVysledku = 1 }
						: new Isir.getIsirWsCuzkDataRequest { ic = ico, filtrAktualniRizeni = Isir.priznakType.F, maxPocetVysledku = 50, maxRelevanceVysledku = 2 }).Result;
		}

		private static void RequestsToWs()
		{
			var client = new Isir.IsirWsCuzkPortTypeClient();

			while (TaskSearch.Status == TaskStatus.Running || !ForDetailRequest.IsEmpty)
			{
				Rizeni rizeni;
				if (ForDetailRequest.TryDequeue(out rizeni))
				{
					Isir.getIsirWsCuzkDataResponse response;
					// retrying until get correct answer :)
					while (true)
					{
						try
						{
							response = MakeWsRequest(client, rizeni.ICO, rizeni.Rc);
							break;
						}
						catch (Exception e)
						{
							AddError("WS", e);
						}
					}

					foreach (var item in response.data)
					{
						if (item.cisloSenatu != rizeni.SpisovaZnacka.SoudniOddeleni || item.bcVec != rizeni.SpisovaZnacka.Cislo || item.rocnik != rizeni.SpisovaZnacka.Rocnik)
						{
							Interlocked.Increment(ref InvalidFileNumber);
						}
						else
						{
							rizeni.AktualniStav = item.druhStavKonkursu;
							rizeni.Url = item.urlDetailRizeni;
							break;
						}
					}

					if (string.IsNullOrEmpty(rizeni.Url))
					{
						Interlocked.Increment(ref Disabled);
					}

					ForStore.Enqueue(rizeni);
				}
				else
				{
					Thread.Sleep(100);
				}
			}

			Thread.Sleep(1000);
		}

		private static void StoreToHlidacStatu()
		{
			var datasetConnector = new DatasetConnector(ApiToken);
			var dataset = InsolvencniRejstrikDataset.InsolvencniRejstrik;

			while (TaskWs.Any(t => t.Status == TaskStatus.Running) || !ForDetailRequest.IsEmpty)
			{
				Rizeni rizeni;
				if (ForStore.TryDequeue(out rizeni))
				{
					// retrying until get correct answer :)
					while (true)
					{
						try
						{
							var result = datasetConnector.AddItemToDataset(dataset, rizeni).Result;
							break;
						}
						catch (Exception e)
						{
							AddError("Store", e);
						}
					}
					Interlocked.Increment(ref StoredItems);
					LastStoredItem = rizeni.ZahajeniRizeni;
				}
				else
				{
					Thread.Sleep(100);
				}
			}
		}

		private static void AddError(string thread, Exception e)
		{
			if (Errors.Count > 10)
			{
				Errors.Remove(Errors.First());
			}
			Errors.Add($"[{DateTime.Now.ToShortTimeString()}] {thread} - {e.Message}");
			Thread.Sleep(1000);
		}

		private static void PrepareDataset(bool removeDatasetIfExists = false, bool updateDatasetIfExists = false)
		{
			var datasetConnector = new DatasetConnector(ApiToken);
			var dataset = InsolvencniRejstrikDataset.InsolvencniRejstrik;

			var datasetExists = datasetConnector.DatasetExists(dataset).Result;
			if (datasetExists && removeDatasetIfExists)
			{
				Console.WriteLine("Maze se stary dataset");
				datasetConnector.DeleteDataset(dataset).Wait();
				datasetExists = false;
			}
			if (datasetExists && updateDatasetIfExists)
			{
				Console.WriteLine("Aktualizuje se dataset");
				Console.WriteLine(" > " + datasetConnector.UpdateDataset(dataset).Result);
			}
			if (!datasetExists)
			{
				Console.WriteLine("Vytvari se novy dataset");
				Console.WriteLine(" > " + datasetConnector.CreateDataset(dataset).Result);
			}
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
		public string AktualniStav { get; set; }
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
