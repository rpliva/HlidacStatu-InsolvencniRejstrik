using System;
using System.Collections.Concurrent;

namespace InsolvencniRejstrik.ByEvents
{
	class RepositoryCache : IRepository
	{
		private readonly IRepository UnderlyingRepository;
		private readonly Func<string, Rizeni> CreateNewInsolvencyProceeding;
		private readonly Func<OsobaId, Osoba> CreateNewPerson;

		private ConcurrentDictionary<string, Rizeni> RizeniCache = new ConcurrentDictionary<string, Rizeni>();
		private ConcurrentDictionary<string, Osoba> OsobaCache = new ConcurrentDictionary<string, Osoba>();

		public RepositoryCache(IRepository repository, Func<string, Rizeni> createNewInsolvencyProceeding, Func<OsobaId, Osoba> createNewPerson)
		{
			UnderlyingRepository = repository;
			CreateNewInsolvencyProceeding = createNewInsolvencyProceeding;
			CreateNewPerson = createNewPerson;
		}

		public Dokument GetDocument(string id) => UnderlyingRepository.GetDocument(id);
		public Rizeni GetInsolvencyProceeding(string spisovaZnacka) => RizeniCache.GetOrAdd(spisovaZnacka, UnderlyingRepository.GetInsolvencyProceeding(spisovaZnacka) ?? CreateNewInsolvencyProceeding(spisovaZnacka));
		public Osoba GetPerson(OsobaId id) => OsobaCache.GetOrAdd(id.GetId(), UnderlyingRepository.GetPerson(id) ?? CreateNewPerson(id));
		public void SetDocument(Dokument item) => UnderlyingRepository.SetDocument(item);
		public void SetInsolvencyProceeding(Rizeni item) => UnderlyingRepository.SetInsolvencyProceeding(item);
		public void SetPerson(Osoba item) => UnderlyingRepository.SetPerson(item);
	}
}
