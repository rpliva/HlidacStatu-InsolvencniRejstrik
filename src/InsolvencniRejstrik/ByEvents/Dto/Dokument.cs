using System;
using Nest;

namespace InsolvencniRejstrik.ByEvents
{
	class Dokument
	{
        [Nest.Keyword]
		public string Id { get; set; }
		[Nest.Keyword]
		public string SpisovaZnacka { get; set; }
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


        public override string ToString() => $"{DatumVlozeni:dd.MM.yyyy} ({Oddil}) - {Popis}";

		protected bool Equals(Dokument other)
		{
			return Equals(Id, other.Id)
				&& Equals(SpisovaZnacka, other.SpisovaZnacka)
				&& Equals(DatumVlozeni, other.DatumVlozeni)
				&& Equals(Popis, other.Popis)
				&& Equals(Url, other.Url)
				&& Equals(Oddil, other.Oddil);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}
			if (ReferenceEquals(this, obj))
			{
				return true;
			}
			if (obj.GetType() != GetType())
			{
				return false;
			}
			return Equals((Dokument)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var result = Id?.GetHashCode() ?? 0;
				result = (result * 397) ^ SpisovaZnacka.GetHashCode();
				result = (result * 397) ^ DatumVlozeni.GetHashCode();
				result = (result * 397) ^ (Popis?.GetHashCode() ?? 0);
				result = (result * 397) ^ (Url?.GetHashCode() ?? 0);
				result = (result * 397) ^ (Oddil?.GetHashCode() ?? 0);
				return result;
			}
		}
	}
}
