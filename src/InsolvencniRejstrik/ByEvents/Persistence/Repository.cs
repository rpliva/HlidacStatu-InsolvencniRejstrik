using System.Collections.Concurrent;

namespace InsolvencniRejstrik.ByEvents
{
	class Repository : IRepository
	{
		private readonly Stats Stats;

		public Repository(Stats stats)
		{
			Stats = stats;
		}

		public long GetLastEventId()
		{
			// musi vratit -1, pokud neni jeste zadny zaznam ulozen, jinka vraci id udalosti posledniho zaznamu
			return 14446367;
		}

		private ConcurrentDictionary<string, Osoba> TempPeople = new ConcurrentDictionary<string, Osoba>();
		public Osoba GetPerson(string id, string idPuvodce)
		{
			Stats.PersonGet++;
			return TempPeople.TryGetValue(id + "_" + idPuvodce, out var o) ? o : null;
		}

		public void SetPerson(Osoba item)
		{
			Stats.PersonSet++;
			TempPeople.AddOrUpdate(item.Id + "_" + item.IdPuvodce, item, (k, o) => item);
		}

		private ConcurrentDictionary<string, Dokument> TempDocuments = new ConcurrentDictionary<string, Dokument>();
		public Dokument GetDocument(string id)
		{
			Stats.DocumentGet++;
			return TempDocuments.TryGetValue(id, out var d) ? d : null;
		}

		public void SetDocument(Dokument item)
		{
			Stats.DocumentSet++;
			TempDocuments.AddOrUpdate(item.Id, item, (k, d) => item);
		}

		private ConcurrentDictionary<string, Rizeni> TempInsolvencyProceeding = new ConcurrentDictionary<string, Rizeni>();
		public Rizeni GetInsolvencyProceeding(string spisovaZnacka)
		{
			Stats.InsolvencyProceedingGet++;
			return TempInsolvencyProceeding.TryGetValue(spisovaZnacka, out var r) ? r : null;
		}

		public void SetInsolvencyProceeding(Rizeni item)
		{
			Stats.InsolvencyProceedingSet++;
			TempInsolvencyProceeding.AddOrUpdate(item.SpisovaZnacka, item, (k, r) => item);
		}
	}
}
