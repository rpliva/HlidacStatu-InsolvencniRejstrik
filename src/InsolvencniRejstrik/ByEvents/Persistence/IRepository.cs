namespace InsolvencniRejstrik.ByEvents
{
	interface IRepository
	{
		long GetLastEventId();
		Dokument GetDocument(string id);
		Osoba GetPerson(string id, string idPuvodce);
		Rizeni GetInsolvencyProceeding(string spisovaZnacka);
		void SetDocument(Dokument item);
		void SetPerson(Osoba item);
		void SetInsolvencyProceeding(Rizeni item);
	}
}
