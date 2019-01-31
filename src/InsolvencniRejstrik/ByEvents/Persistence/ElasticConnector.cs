using Nest;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace InsolvencniRejstrik.ByEvents
{
	class ElasticConnector
	{
		private readonly string[] ElasticIndexNames = new[] { "insolvencnirestrik-dokument", "insolvencnirestrik-osoba2", "insolvencnirestrik-rizeni2" };

		private static readonly object LockRoot = new object();
		private Dictionary<string, ElasticClient> Clients = new Dictionary<string, ElasticClient>();

		public ElasticClient GetESClient(Database db, int timeOut = 60000, int connectionLimit = 80)
		{
			var idxname = ElasticIndexNames[(int)db];
			var cnnset = $"{idxname}|{timeOut}|{connectionLimit}";
			if (!Clients.ContainsKey(cnnset))
			{
				lock (LockRoot)
				{
					if (!Clients.ContainsKey(cnnset))
					{
						var settings = GetElasticSearchConnectionSettings(idxname, timeOut, connectionLimit);
						var client = new ElasticClient(settings);
						CreateElasticIndex(db, client);
						Clients.Add(cnnset, client);
					}
				}
			}
			return Clients[cnnset];
		}

		private ConnectionSettings GetElasticSearchConnectionSettings(string indexName, int timeOut = 60000, int? connectionLimit = null)
		{
			var esUrl = GetConfigValue("ESConnection");
			var settings = new ConnectionSettings(new Uri(esUrl))
				.DefaultIndex(indexName)
				.DisableAutomaticProxyDetection(false)
				.RequestTimeout(TimeSpan.FromMilliseconds(timeOut))
				.SniffLifeSpan(null)
				//.OnRequestCompleted(call =>
				//{
				//	// log out the request and the request body, if one exists for the type of request
				//	if (call.RequestBodyInBytes != null)
				//	{
				//		//logger.Debug($"{call.HttpMethod}\t{call.Uri}\t" +
				//		//    $"{Encoding.UTF8.GetString(call.RequestBodyInBytes)}");
				//	}
				//	else
				//	{
				//		//logger.Debug($"{call.HttpMethod}\t{call.Uri}\t");
				//	}
				//})
				;

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

		private string GetConfigValue(string value)
		{
			return ConfigurationManager.AppSettings[value] ?? string.Empty;
		}

		private void CreateElasticIndex(Database db, ElasticClient client)
		{
			var ret = client.IndexExists(client.ConnectionSettings.DefaultIndex);
			if (!ret.Exists)
			{
				var set = new IndexSettings
				{
					NumberOfReplicas = 2,
					NumberOfShards = 25
				};
				var an = new CustomAnalyzer
				{
					Tokenizer = "standard",
					Filter = new[] { "lowercase", "czech_stop", "czech_stemmer", "asciifolding" }
				};
				set.Analysis = new Analysis()
				{
					Analyzers = new Analyzers(),
					TokenFilters = new TokenFilters(),
				};
				set.Analysis.Analyzers.Add("default", an);
				set.Analysis.TokenFilters.Add("czech_stop", new StopTokenFilter() { StopWords = new string[] { "_czech_" } });
				set.Analysis.TokenFilters.Add("czech_stemmer", new StemmerTokenFilter() { Language = "czech" });
				var idxSt = new IndexState { Settings = set };

				var res = client
				   .CreateIndex(client.ConnectionSettings.DefaultIndex, i => i
					   .InitializeUsing(idxSt)
					   .Mappings(m =>
					   {
						   switch (db)
						   {
							   case Database.Dokument:
								   return m.Map<Dokument>(map => map.AutoMap().DateDetection(false));
							   case Database.Osoba:
								   return m.Map<Osoba>(map => map.AutoMap().DateDetection(false));
							   case Database.Rizeni:
								   return m.Map<Rizeni>(map => map.AutoMap().DateDetection(false));
							   default:
								   throw new ArgumentOutOfRangeException($"Unknown DB type {db.ToString()}");
						   }
					   })
				   );
			}
		}
	}

	public enum Database
	{
		Dokument = 0,
		Osoba = 1,
		Rizeni = 2
	}
}
