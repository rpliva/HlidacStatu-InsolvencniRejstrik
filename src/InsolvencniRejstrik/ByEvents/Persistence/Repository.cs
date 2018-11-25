using Nest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public long GetLastEventId()
        {
            // musi vratit -1, pokud neni jeste zadny zaznam ulozen, jinka vraci id udalosti posledniho zaznamu

            //TODO ukazka, jak to udelat pomoci Elastic, pokud by to bylo max cislo z rady dokumentu
            /*
            var res = GetESClient(Database.Dokument)
                .Search<Dokument>(s => s
                    .Sort(ss => ss.Descending(ff => ff.Id))
                    .Size(1)
                );
            if (res.IsValid)
            {
                if (res.Total == 0)
                    return -1;
                else
                    return (long)res.Hits.First().Source.Id;
            }
            throw new Elasticsearch.Net.ElasticsearchClientException(res.ServerError?.ToString());
            */

            return 14446367;
        }

        private ConcurrentDictionary<string, Osoba> TempPeople = new ConcurrentDictionary<string, Osoba>();
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
                return res.Hits.FirstOrDefault()?.Source;
            }
            throw new Elasticsearch.Net.ElasticsearchClientException(res.ServerError?.ToString());

            Stats.PersonGet++;
            return TempPeople.TryGetValue(id + "_" + idPuvodce, out var o) ? o : null;
        }

        public void SetPerson(Osoba item)
        {
            var res = GetESClient(Database.Osoba)
                .Index<Osoba>(item, o => o.Id(item.Id.ToString()));

            if (!res.IsValid)
                throw new Elasticsearch.Net.ElasticsearchClientException(res.ServerError?.ToString());


            Stats.PersonSet++;
            TempPeople.AddOrUpdate(item.Id + "_" + item.IdPuvodce, item, (k, o) => item);
        }

        private ConcurrentDictionary<string, Dokument> TempDocuments = new ConcurrentDictionary<string, Dokument>();
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
                return res.Hits.FirstOrDefault()?.Source;
            }
            throw new Elasticsearch.Net.ElasticsearchClientException(res.ServerError?.ToString());


            Stats.DocumentGet++;
            return TempDocuments.TryGetValue(id, out var d) ? d : null;
        }

        public void SetDocument(Dokument item)
        {

            var res = GetESClient(Database.Dokument)
                .Index<Dokument>(item, o => o.Id(item.Id.ToString()));

            if (!res.IsValid)
                throw new Elasticsearch.Net.ElasticsearchClientException(res.ServerError?.ToString());


            Stats.DocumentSet++;
            TempDocuments.AddOrUpdate(item.Id, item, (k, d) => item);
        }

        private ConcurrentDictionary<string, Rizeni> TempInsolvencyProceeding = new ConcurrentDictionary<string, Rizeni>();
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
                return res.Hits.FirstOrDefault()?.Source;
            }
            throw new Elasticsearch.Net.ElasticsearchClientException(res.ServerError?.ToString());



            Stats.InsolvencyProceedingGet++;
            return TempInsolvencyProceeding.TryGetValue(spisovaZnacka, out var r) ? r : null;
        }

        public void SetInsolvencyProceeding(Rizeni item)
        {

            var res = GetESClient(Database.Rizeni)
                .Index<Rizeni>(item, o => o.Id(item.SpisovaZnacka.ToString())); //druhy parametr musi byt pole, ktere je unikatni

            if (!res.IsValid)
                throw new Elasticsearch.Net.ElasticsearchClientException(res.ServerError?.ToString());

            Stats.InsolvencyProceedingSet++;
            TempInsolvencyProceeding.AddOrUpdate(item.SpisovaZnacka, item, (k, r) => item);
        }


        public static string GetConfigValue(string value)
        {
            string @out = System.Configuration.ConfigurationManager.AppSettings[value];
            if (@out == null)
            {
                @out = string.Empty;
            }
            return @out;
        }
        public static ConnectionSettings GetElasticSearchConnectionSettings(string indexName, int timeOut = 60000, int? connectionLimit = null)
        {

            string esUrl = GetConfigValue("ESConnection");
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


                })
                ;

            if (System.Diagnostics.Debugger.IsAttached || GetConfigValue("ESDebugDataEnabled") == "true")
                settings = settings.DisableDirectStreaming();

            if (connectionLimit.HasValue)
                settings = settings.ConnectionLimit(connectionLimit.Value);

            return settings;


        }

        public static void CreateElasticIndex(ElasticClient client)
        {
            var ret = client.IndexExists(client.ConnectionSettings.DefaultIndex);
            if (ret.Exists == false)
            {
                IndexSettings set = new IndexSettings();
                set.NumberOfReplicas = 2;
                set.NumberOfShards = 25;
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
                set.Analysis = new Nest.Analysis()
                {
                    Analyzers = new Analyzers(),
                    TokenFilters = new TokenFilters(),
                };

                set.Analysis.Analyzers.Add("default", an);
                set.Analysis.TokenFilters.Add("czech_stop", new StopTokenFilter() { StopWords = new string[] { "_czech_" } });
                set.Analysis.TokenFilters.Add("czech_stemmer", new StemmerTokenFilter() { Language = "czech" });
                IndexState idxSt = new IndexState();
                idxSt.Settings = set;

                Nest.ICreateIndexResponse res = client
                   .CreateIndex(client.ConnectionSettings.DefaultIndex, i => i
                       .InitializeUsing(idxSt)
                       .Mappings(m => m
                           .Map<InsolvencniRejstrik.ByEvents.Dokument>(map => map.AutoMap().DateDetection(false))
                           )
                   );
            }
            else
                throw new System.InvalidOperationException($"Index {client.ConnectionSettings.DefaultIndex} already exists");
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
                string cnnset = string.Format("{0}|{1}|{2}", idxname, timeOut, connectionLimit);
                ConnectionSettings sett = GetElasticSearchConnectionSettings(idxname, timeOut, connectionLimit);
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
                CreateElasticIndex(client);
        }


        public enum Database
        {
            Dokument,
            Osoba,
            Rizeni
        }

    }
}
