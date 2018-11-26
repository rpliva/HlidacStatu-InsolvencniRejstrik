using Elasticsearch.Net;
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

		public Osoba GetPerson(string id, string idPuvodce)
		{
			var res = Elastic.GetESClient(Database.Osoba)
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
			var res = Elastic.GetESClient(Database.Osoba).Index(item, o => o.Id(item.Id.ToString()));
			if (!res.IsValid)
			{
				throw new ElasticsearchClientException(res.ServerError?.ToString());
			}
			Stats.PersonSet++;
		}

		public Dokument GetDocument(string id)
		{
			var res = Elastic.GetESClient(Database.Dokument)
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
			var res = Elastic.GetESClient(Database.Dokument).Index(item, o => o.Id(item.Id.ToString()));
			if (!res.IsValid)
			{
				throw new ElasticsearchClientException(res.ServerError?.ToString());
			}
			Stats.DocumentSet++;
		}

		public Rizeni GetInsolvencyProceeding(string spisovaZnacka)
		{
			var res = Elastic.GetESClient(Database.Rizeni)
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
			var res = Elastic.GetESClient(Database.Rizeni).Index(item, o => o.Id(item.SpisovaZnacka.ToString())); //druhy parametr musi byt pole, ktere je unikatni
			if (!res.IsValid)
			{
				throw new ElasticsearchClientException(res.ServerError?.ToString());
			}
			Stats.InsolvencyProceedingSet++;
		}
	}
}
