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

namespace InsolvencniRejstrik.FromSearch
{
	class SearchConnector : BaseConnector
	{
		private readonly string ApiToken;

		public SearchConnector(string apiToken)
		{
			ApiToken = apiToken;
		}

		public void Handle(DateTime startDate)
		{
			PrepareDataset();

			Console.WriteLine("Pripravuje se prostredi");
			TaskSearch = RunTask(() => SearchInsolvencyProceedings(startDate, DateTime.Now.AddDays(-1)));
			TaskWs = RunTasks(WsThreads, () => RequestsToWs());
			TaskStore = RunTasks(StoreThreads, () => StoreToHlidacStatu());

			var start = DateTime.Now;

			while (TaskStore.Any(t => t.Status == TaskStatus.Running))
			{
				PrintHeader();
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
				catch (Exception)
				{
					// do nothing
				}

				Thread.Sleep(1000);
			}
		}

		private DateTime LastFinishedPeriodInSearch = DateTime.MinValue;
		private DateTime LastStoredItem = DateTime.MinValue;
		private int WsThreads = 4;
		private int StoreThreads = 4;
		private int ReadFromSearch = 0;
		private int InvalidFileNumber = 0;
		private int StoredItems = 0;
		private int Disabled = 0;
		private Task TaskSearch;
		private Task[] TaskWs;
		private Task[] TaskStore;
		private ConcurrentQueue<Rizeni> ForDetailRequest = new ConcurrentQueue<Rizeni>();
		private ConcurrentQueue<Rizeni> ForStore = new ConcurrentQueue<Rizeni>();
		private List<string> Errors = new List<string>();

		private HtmlNode MakeSearchRequest(HtmlWeb client, DateTime fromEndOfPeriodDate)
		{
			var content = client.Load($"https://isir.justice.cz/isir/ueu/vysledek_lustrace.do?aktualnost=AKTUALNI_I_UKONCENA&spis_znacky_datum={fromEndOfPeriodDate.ToString("dd.MM.yyyy")}&spis_znacky_obdobi=14DNI");
			return content.DocumentNode.Descendants("table").Where(t => t.Attributes["class"]?.Value == "vysledekLustrace").Skip(1).Single();
		}

		private void SearchInsolvencyProceedings(DateTime fromEndOfPeriodDate, DateTime until)
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

		private Isir.getIsirWsCuzkDataResponse MakeWsRequest(Isir.IsirWsCuzkPortTypeClient wsClient, string ico, string rc)
		{
			return wsClient.getIsirWsCuzkDataAsync(string.IsNullOrEmpty(ico)
						? new Isir.getIsirWsCuzkDataRequest { rc = rc, filtrAktualniRizeni = Isir.priznakType.F, maxPocetVysledku = 50, maxRelevanceVysledku = 1 }
						: new Isir.getIsirWsCuzkDataRequest { ic = ico, filtrAktualniRizeni = Isir.priznakType.F, maxPocetVysledku = 50, maxRelevanceVysledku = 2 }).Result;
		}

		private void RequestsToWs()
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

		private void StoreToHlidacStatu()
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
							var result = datasetConnector.AddItemToDataset<Rizeni>(dataset, rizeni).Result;
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

		private void AddError(string thread, Exception e)
		{
			if (Errors.Count > 10)
			{
				Errors.Remove(Errors.First());
			}
			Errors.Add($"[{DateTime.Now.ToShortTimeString()}] {thread} - {e.Message}");
			Thread.Sleep(1000);
		}

		private void PrepareDataset(bool removeDatasetIfExists = false, bool updateDatasetIfExists = false)
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
}
