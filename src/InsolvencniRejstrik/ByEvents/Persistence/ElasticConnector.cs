using Nest;
using System;
using System.Configuration;

namespace InsolvencniRejstrik.ByEvents
{
	class ElasticConnector
	{
		private readonly string ElasticIndexName = "insolvencnirestrik";

		private static readonly object LockRoot = new object();
		private ElasticClient Client;

		public ElasticClient GetESClient(int timeOut = 60000, int connectionLimit = 80)
		{
			var cnnset = $"{ElasticIndexName}|{timeOut}|{connectionLimit}";
			if (Client == null)
			{
				lock (LockRoot)
				{
					if (Client == null)
					{
						var settings = GetElasticSearchConnectionSettings(ElasticIndexName, timeOut, connectionLimit);
						Client = new ElasticClient(settings);
						CreateElasticIndex();
					}
				}
			}
			return Client;
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

		private void CreateElasticIndex()
		{
			var ret = Client.IndexExists(Client.ConnectionSettings.DefaultIndex);
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

				var res = Client
				   .CreateIndex(Client.ConnectionSettings.DefaultIndex, i => i
					   .InitializeUsing(idxSt)
					   .Mappings(m => m.Map<Rizeni>(map => map.AutoMap().DateDetection(false)))
				   );
			}
		}
	}
}
