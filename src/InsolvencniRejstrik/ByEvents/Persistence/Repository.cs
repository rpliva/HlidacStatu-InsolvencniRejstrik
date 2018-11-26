using Elasticsearch.Net;
using Nest;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace InsolvencniRejstrik.ByEvents
{
	class Repository : IRepository
	{
		private readonly Stats Stats;

		private static readonly string elasticIndexNameDokument = "insolvencnirestrik-dokument";
		private static readonly string elasticIndexNameOsoba = "insolvencnirestrik-osoba";
		private static readonly string elasticIndexNameRizeni = "insolvencnirestrik-rizeni";


		public Repository(Stats stats)
		{
			Stats = stats;
		}

		public Osoba GetPerson(string id, string idPuvodce)
		{
			var res = GetESClient(Database.Osoba)
				.Search<Osoba>(s => s
					.Size(1) //zrus, pokud ma vratit vice zaznamu
					.Query(q => q
					.Bool(b => b.Must(
							m => m.Term(t => t.Field(f => f.Id).Value(id))
							,
							m => m.Term(t => t.Field(f => f.IdPuvodce).Value(idPuvodce))
						  )
						)
					)
				);
			if (res.IsValid)
			{
				Stats.PersonGet++;
				return res.Hits.FirstOrDefault()?.Source;
			}
			throw new ElasticsearchClientException(res.ServerError?.ToString());
		}

		public void SetPerson(Osoba item)
		{
			var res = GetESClient(Database.Osoba).Index(item, o => o.Id(item.Id.ToString()));
			if (!res.IsValid)
			{
				throw new ElasticsearchClientException(res.ServerError?.ToString());
			}
			Stats.PersonSet++;
		}

		public Dokument GetDocument(string id)
		{
			var res = GetESClient(Database.Dokument)
				.Search<Dokument>(s => s
					.Size(1) //zrus, pokud ma vratit vice zaznamu
					.Query(q => q
					.Term(t => t.Field(f => f.Id).Value(id))
					)
				);
			if (res.IsValid)
			{
				Stats.DocumentGet++;
				return res.Hits.FirstOrDefault()?.Source;
			}
			throw new ElasticsearchClientException(res.ServerError?.ToString());
		}

		public void SetDocument(Dokument item)
		{
			var res = GetESClient(Database.Dokument).Index(item, o => o.Id(item.Id.ToString()));
			if (!res.IsValid)
			{
				throw new ElasticsearchClientException(res.ServerError?.ToString());
			}
			Stats.DocumentSet++;
		}

		public Rizeni GetInsolvencyProceeding(string spisovaZnacka)
		{
			var res = GetESClient(Database.Rizeni)
				.Search<Rizeni>(s => s
					.Size(1) //zrus, pokud ma vratit vice zaznamu
					.Query(q => q
					.Term(t => t.Field(f => f.SpisovaZnacka).Value(spisovaZnacka))
					)
				);
			if (res.IsValid)
			{
				Stats.InsolvencyProceedingGet++;
				return res.Hits.FirstOrDefault()?.Source;
			}
			throw new ElasticsearchClientException(res.ServerError?.ToString());
		}

		public void SetInsolvencyProceeding(Rizeni item)
		{
			var res = GetESClient(Database.Rizeni).Index(item, o => o.Id(item.SpisovaZnacka.ToString())); //druhy parametr musi byt pole, ktere je unikatni
			if (!res.IsValid)
			{
				throw new ElasticsearchClientException(res.ServerError?.ToString());
			}
			Stats.InsolvencyProceedingSet++;
		}

		public static string GetConfigValue(string value)
		{
			return ConfigurationManager.AppSettings[value] ?? string.Empty;
		}

		public static ConnectionSettings GetElasticSearchConnectionSettings(string indexName, int timeOut = 60000, int? connectionLimit = null)
		{
			var esUrl = GetConfigValue("ESConnection");
			var settings = new ConnectionSettings(new Uri(esUrl))
				.DefaultIndex(indexName)
				.DisableAutomaticProxyDetection(false)
				.RequestTimeout(TimeSpan.FromMilliseconds(timeOut))
				.SniffLifeSpan(null)
				.OnRequestCompleted(call =>
				{
					// log out the request and the request body, if one exists for the type of request
					if (call.RequestBodyInBytes != null)
					{
						//logger.Debug($"{call.HttpMethod}\t{call.Uri}\t" +
						//    $"{Encoding.UTF8.GetString(call.RequestBodyInBytes)}");
					}
					else
					{
						//logger.Debug($"{call.HttpMethod}\t{call.Uri}\t");
					}
				});

			if (System.Diagnostics.Debugger.IsAttached || GetConfigValue("ESDebugDataEnabled") == "true")
			{
				settings = settings.DisableDirectStreaming();
			}

			if (connectionLimit.HasValue)
			{
				settings = settings.ConnectionLimit(connectionLimit.Value);
			}

			return settings;
		}

		public static void CreateElasticIndex(ElasticClient client)
		{
			var ret = client.IndexExists(client.ConnectionSettings.DefaultIndex);
			if (ret.Exists == false)
			{
				var set = new IndexSettings
				{
					NumberOfReplicas = 2,
					NumberOfShards = 25
				};
				// Create a Custom Analyzer ...
				var an = new CustomAnalyzer();
				an.Tokenizer = "standard";
				// ... with Filters from the StandardAnalyzer
				var filter = new List<string>();
				filter.Add("lowercase");
				filter.Add("czech_stop");
				filter.Add("czech_stemmer");
				filter.Add("asciifolding");
				an.Filter = filter;
				// Add the Analyzer with a name
				set.Analysis = new Analysis()
				{
					Analyzers = new Analyzers(),
					TokenFilters = new TokenFilters(),
				};

				set.Analysis.Analyzers.Add("default", an);
				set.Analysis.TokenFilters.Add("czech_stop", new StopTokenFilter() { StopWords = new string[] { "_czech_" } });
				set.Analysis.TokenFilters.Add("czech_stemmer", new StemmerTokenFilter() { Language = "czech" });
				var idxSt = new IndexState();
				idxSt.Settings = set;

				var res = client
				   .CreateIndex(client.ConnectionSettings.DefaultIndex, i => i
					   .InitializeUsing(idxSt)
					   .Mappings(m => m
						   .Map<Dokument>(map => map.AutoMap().DateDetection(false))
						   )
				   );
			}
			else
			{
				throw new InvalidOperationException($"Index {client.ConnectionSettings.DefaultIndex} already exists");
			}
		}


		private static object _clientLock = new object();
		private static Dictionary<string, ElasticClient> _clients = new Dictionary<string, ElasticClient>();
		public static ElasticClient GetESClient(Database db, int timeOut = 60000, int connectionLimit = 80)
		{
			string idxname = null;
			switch (db)
			{
				case Database.Dokument:
					idxname = elasticIndexNameDokument;
					break;
				case Database.Osoba:
					idxname = elasticIndexNameOsoba;
					break;
				case Database.Rizeni:
					idxname = elasticIndexNameRizeni;
					break;
				default:
					throw new ArgumentOutOfRangeException("db");
			}
			lock (_clientLock)
			{
				var cnnset = $"{idxname}|{timeOut}|{connectionLimit}";
				var sett = GetElasticSearchConnectionSettings(idxname, timeOut, connectionLimit);
				if (!_clients.ContainsKey(cnnset))
				{
					var _client = new ElasticClient(sett);
					InitElasticSearchIndex(_client);
					_clients.Add(cnnset, _client);
				}
				return _clients[cnnset];
			}
		}

		public static void InitElasticSearchIndex(ElasticClient client)
		{
			var ret = client.IndexExists(client.ConnectionSettings.DefaultIndex);
			if (ret.Exists == false)
			{
				CreateElasticIndex(client);
			}
		}

		public enum Database
		{
			Dokument,
			Osoba,
			Rizeni
		}
	}
}
