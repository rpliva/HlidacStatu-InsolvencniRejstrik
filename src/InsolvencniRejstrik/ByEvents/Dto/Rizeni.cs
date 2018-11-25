using System;
using System.Collections.Generic;

namespace InsolvencniRejstrik.ByEvents
{
	class Rizeni
	{
        [Nest.Keyword]
        public string SpisovaZnacka { get; set; }
        [Nest.Keyword]
        public string Stav { get; set; }
        [Nest.Date]
        public DateTime? Vyskrtnuto { get; set; }
        [Nest.Keyword]
        public string Url { get; set; }

		public override string ToString() => $"{SpisovaZnacka} - {Stav}";

		public string Dump()
		{
			return $"{SpisovaZnacka};{Stav};{Url}";
		}

		protected bool Equals(Rizeni other)
		{
			return Equals(SpisovaZnacka, other.SpisovaZnacka)
				&& Equals(Stav, other.Stav)
				&& Equals(Vyskrtnuto, other.Vyskrtnuto)
				&& Equals(Url, other.Url);
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
			return Equals((Rizeni)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var result = SpisovaZnacka?.GetHashCode() ?? 0;
				result = (result * 397) ^ Stav?.GetHashCode() ?? 0;
				result = (result * 397) ^ (Vyskrtnuto?.GetHashCode() ?? 0);
				result = (result * 397) ^ (Url?.GetHashCode() ?? 0);
				return result;
			}
		}
	}
}
