using System;

namespace InsolvencniRejstrik.ByEvents
{
	public class Osoba
	{
        [Nest.Keyword]
        public string IdPuvodce { get; set; }
        [Nest.Keyword]
        public string Id { get; set; }
        [Nest.Text]
        public string Nazev { get; set; }
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

		public override string ToString() => Typ == "P"
			? $"{IdPuvodce}: {Nazev} - {Role} (ic: {ICO})"
			: $"{IdPuvodce}: {Nazev} - {Role} (rc: {Rc}, {(DatumNarozeni.HasValue ? DatumNarozeni.Value.ToShortDateString() : "-")})";

		protected bool Equals(Osoba other)
		{
			return Equals(Id, other.Id)
				&& Equals(IdPuvodce, other.IdPuvodce)
				&& Equals(Nazev, other.Nazev)
				&& Equals(Role, other.Role)
				&& Equals(Typ, other.Typ)
				&& Equals(ICO, other.ICO)
				&& Equals(Rc, other.Rc)
				&& Equals(DatumNarozeni, other.DatumNarozeni);
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
			return Equals((Osoba)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var result = Id?.GetHashCode() ?? 0;
				result = (result * 397) ^ (IdPuvodce?.GetHashCode() ?? 0);
				result = (result * 397) ^ (Nazev?.GetHashCode() ?? 0);
				result = (result * 397) ^ (Role?.GetHashCode() ?? 0);
				result = (result * 397) ^ (Typ?.GetHashCode() ?? 0);
				result = (result * 397) ^ (ICO?.GetHashCode() ?? 0);
				result = (result * 397) ^ (Rc?.GetHashCode() ?? 0);
				result = (result * 397) ^ (DatumNarozeni?.GetHashCode() ?? 0);
				return result;
			}
		}

	}
}
