using Elasticsearch.Net;
using System;
using System.Linq;

namespace InsolvencniRejstrik.ByEvents
{
	class Repository : IRepository
	{
		private readonly ElasticConnector Elastic;

		private readonly Stats Stats;

		public Repository(Stats stats)
		{
			Stats = stats;
			Elastic = new ElasticConnector();
		}
		public Rizeni GetInsolvencyProceeding(string spisovaZnacka, Func<string, Rizeni> createNewInsolvencyProceeding)
		{
			var res = Elastic.GetESClient()
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
			var res = Elastic.GetESClient().Index(item, o => o.Id(item.SpisovaZnacka.ToString())); //druhy parametr musi byt pole, ktere je unikatni
			if (!res.IsValid)
			{
				throw new ElasticsearchClientException(res.ServerError?.ToString());
			}
			Stats.InsolvencyProceedingSet++;
		}
	}
}
