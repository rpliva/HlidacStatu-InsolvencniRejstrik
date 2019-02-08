using System;
using System.Collections.Generic;
using System.Linq;

namespace InsolvencniRejstrik.ByEvents
{
	public class Rizeni
	{
		public Rizeni()
		{
			Subjekty = new List<Subjekt>();
		}

		[Nest.Keyword]
		public string SpisovaZnacka { get; set; }
		[Nest.Keyword]
		public string Stav { get; set; }
		[Nest.Date]
		public DateTime? Vyskrtnuto { get; set; }
		[Nest.Keyword]
		public string Url { get; set; }
		[Nest.Nested]
		public List<Subjekt> Subjekty { get; set; }
		[Nest.Date]
		public DateTime? DatumZalozeni { get; set; }
		[Nest.Date]
		public DateTime PosledniZmena { get; set; }
		[Nest.Keyword]
		public string Soud { get; set; }


		public override string ToString() => $"{SpisovaZnacka} - {Stav}";

		protected bool Equals(Rizeni other)
		{
			return Equals(SpisovaZnacka, other.SpisovaZnacka)
				&& Equals(Stav, other.Stav)
				&& Equals(Vyskrtnuto, other.Vyskrtnuto)
				&& Equals(Url, other.Url)
				&& Equals(DatumZalozeni, other.DatumZalozeni)
				&& Equals(PosledniZmena, other.PosledniZmena)
				&& Equals(Soud, other.Soud)
				&& Enumerable.SequenceEqual(Subjekty, other.Subjekty);
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
				result = (result * 397) ^ (DatumZalozeni?.GetHashCode() ?? 0);
				result = (result * 397) ^ PosledniZmena.GetHashCode();
				result = (result * 397) ^ Soud?.GetHashCode() ?? 0;
				foreach (var item in Subjekty ?? new List<Subjekt>())
				{
					result = (result * 397) ^ item.GetHashCode();
				}
				return result;
			}
		}
	}

	public class Subjekt
	{
		[Nest.Keyword]
		public string Nazev { get; set; }
		[Nest.Keyword]
		public string ICO { get; set; }
		[Nest.Keyword]
		public string Rc { get; set; }
		[Nest.Keyword]
		public string Role { get; set; }

		protected bool Equals(Subjekt other)
		{
			return Equals(Nazev, other.Nazev)
				&& Equals(ICO, other.ICO)
				&& Equals(Rc, other.Rc)
				&& Equals(Role, other.Role);
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
			return Equals((Subjekt)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var result = Nazev?.GetHashCode() ?? 0;
				result = (result * 397) ^ ICO?.GetHashCode() ?? 0;
				result = (result * 397) ^ Rc?.GetHashCode() ?? 0;
				result = (result * 397) ^ Role?.GetHashCode() ?? 0;
				return result;
			}
		}
	}

}
