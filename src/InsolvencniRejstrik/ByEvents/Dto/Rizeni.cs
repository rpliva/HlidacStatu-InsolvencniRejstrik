using System;
using System.Collections.Generic;

namespace InsolvencniRejstrik.ByEvents
{
	public class Rizeni
	{
		public Rizeni()
		{
			Dokumenty = new List<Dokument>();
			Dluznici = new List<Osoba>();
			Veritele = new List<Osoba>();
			Spravci = new List<Osoba>();
		}

		[Nest.Keyword]
		public string SpisovaZnacka { get; set; }
		[Nest.Keyword]
		public string Stav { get; set; }
		[Nest.Date]
		public DateTime? Vyskrtnuto { get; set; }
		[Nest.Keyword]
		public string Url { get; set; }
		[Nest.Date]
		public DateTime? DatumZalozeni { get; set; }
		[Nest.Date]
		public DateTime PosledniZmena { get; set; }
		[Nest.Keyword]
		public string Soud { get; set; }
		[Nest.Object]
		public List<Dokument> Dokumenty { get; set; }
		[Nest.Object]
		public List<Osoba> Dluznici { get; set; }
		[Nest.Object]
		public List<Osoba> Veritele { get; set; }
		[Nest.Object]
		public List<Osoba> Spravci { get; set; }

		public string UrlId() => SpisovaZnacka.Replace(" ", "_").Replace("/", "-");
	}

	public class Osoba
	{
		[Nest.Keyword]
		public string IdPuvodce { get; set; }
		[Nest.Keyword]
		public string IdOsoby { get; set; }
		[Nest.Text]
		public string PlneJmeno { get; set; }
		[Nest.Keyword]
		public string Role { get; set; }
		[Nest.Keyword]
		public string Typ { get; set; }
		[Nest.Keyword]
		public string ICO { get; set; }
		[Nest.Keyword]
		public string Rc { get; set; }
		[Nest.Date]
		public DateTime? DatumNarozeni { get; set; }
		[Nest.Keyword]
		public string Mesto { get; set; }
		[Nest.Keyword]
		public string Okres { get; set; }
		[Nest.Keyword]
		public string Zeme { get; set; }
		[Nest.Keyword]
		public string Psc { get; set; }
	}

	public class Dokument
	{
		[Nest.Keyword]
		public string Id { get; set; }
		[Nest.Date]
		public DateTime DatumVlozeni { get; set; }
		[Nest.Text]
		public string Popis { get; set; }
		[Nest.Keyword]
		public string Url { get; set; }
		[Nest.Keyword]
		public string Oddil { get; set; }
		[Nest.Text]
		public string PlainText { get; set; }
	}

}
