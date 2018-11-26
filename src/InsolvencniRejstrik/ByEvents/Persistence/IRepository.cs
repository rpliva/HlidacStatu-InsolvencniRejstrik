namespace InsolvencniRejstrik.ByEvents
{
	interface IRepository
	{
		Dokument GetDocument(string id);
		Osoba GetPerson(string id, string idPuvodce);
		Rizeni GetInsolvencyProceeding(string spisovaZnacka);
		void SetDocument(Dokument item);
		void SetPerson(Osoba item);
		void SetInsolvencyProceeding(Rizeni item);
	}
}
