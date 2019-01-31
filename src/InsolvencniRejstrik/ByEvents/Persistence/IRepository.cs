namespace InsolvencniRejstrik.ByEvents
{
	interface IRepository
	{
		Dokument GetDocument(string id);
		Osoba GetPerson(OsobaId id);
		Rizeni GetInsolvencyProceeding(string spisovaZnacka);
		void SetDocument(Dokument item);
		void SetPerson(Osoba item);
		void SetInsolvencyProceeding(Rizeni item);
	}
}
