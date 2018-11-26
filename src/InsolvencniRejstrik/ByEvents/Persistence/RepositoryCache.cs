using System;
using System.Collections.Concurrent;

namespace InsolvencniRejstrik.ByEvents
{
	class RepositoryCache : IRepository
	{
		private readonly IRepository UnderlyingRepository;
		private readonly ConcurrentDictionary<string, Dokument> Documents = new ConcurrentDictionary<string, Dokument>();
		private readonly ConcurrentDictionary<string, Osoba> People = new ConcurrentDictionary<string, Osoba>();
		private readonly ConcurrentDictionary<string, Rizeni> InsolvencyProceedings = new ConcurrentDictionary<string, Rizeni>();
		private readonly Stats Stats;

		public RepositoryCache(IRepository repository, Stats stats)
		{
			UnderlyingRepository = repository;
			Stats = stats;
		}

		public Dokument GetDocument(string id)
		{
			Stats.DocumentCacheGet++;
			return Documents.GetOrAdd(id, UnderlyingRepository.GetDocument);
		}

		public void SetDocument(Dokument item)
		{
			Stats.DocumentCacheSet++;
			if (!Documents.TryGetValue(item.Id, out var doc) || doc != item)
			{
				UnderlyingRepository.SetDocument(item);
				if (doc != null)
				{
					while (!Documents.TryRemove(item.Id, out var tmp))
					{
						Console.WriteLine($"[Cache] Retry removing a document from cache");
					}
				}
			}
		}

		public Osoba GetPerson(string id, string idPuvodce)
		{
			Stats.PersonCacheGet++;
			return People.GetOrAdd(GetPersonKey(id, idPuvodce), a => UnderlyingRepository.GetPerson(id, idPuvodce));
		}

		private string GetPersonKey(string id, string idPuvodce)
		{
			return $"ID-${id}-${idPuvodce}";
		}

		public void SetPerson(Osoba item)
		{
			Stats.PersonCacheSet++;
			if (!People.TryGetValue(GetPersonKey(item.Id, item.IdPuvodce), out var person) || person != item)
			{
				UnderlyingRepository.SetPerson(item);
				if (person != null)
				{
					while (!People.TryRemove(GetPersonKey(item.Id, item.IdPuvodce), out var tmp))
					{
						Console.WriteLine($"[Cache] Retry removing a person from cache");
					}
				}
			}
		}

		public Rizeni GetInsolvencyProceeding(string spisovaZnacka)
		{
			Stats.InsolvencyProceedingCacheGet++;
			return InsolvencyProceedings.GetOrAdd(spisovaZnacka, UnderlyingRepository.GetInsolvencyProceeding);
		}

		public void SetInsolvencyProceeding(Rizeni item)
		{
			Stats.InsolvencyProceedingCacheSet++;
			if (!InsolvencyProceedings.TryGetValue(item.SpisovaZnacka, out var proceeding) || proceeding != item)
			{
				UnderlyingRepository.SetInsolvencyProceeding(item);
				if (proceeding != null)
				{
					while (!InsolvencyProceedings.TryRemove(item.SpisovaZnacka, out var tmp))
					{
						Console.WriteLine($"[Cache] Retry removing an insolvency proceeding from cache");
					}
				}
			}
		}
	}
}
