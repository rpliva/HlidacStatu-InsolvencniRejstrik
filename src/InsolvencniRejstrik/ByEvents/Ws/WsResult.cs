using System;

namespace InsolvencniRejstrik.ByEvents
{
	class WsResult
	{
		public long Id { get; set; }
		public string DokumentUrl { get; set; }
		public string TypUdalosti { get; set; }
		public string PopisUdalosti { get; set; }
		public string SpisovaZnacka { get; set; }
		public string Oddil { get; set; }
		public string Poznamka { get; set; }
		public DateTime DatumZalozeniUdalosti { get; set; }

		public static WsResult From(IsirWs.isirWsPublicData item)
		{
			return new WsResult
			{
				Id = item.id,
				SpisovaZnacka = item.spisovaZnacka,
				TypUdalosti = item.typUdalosti,
				PopisUdalosti = item.popisUdalosti,
				DatumZalozeniUdalosti = item.datumZalozeniUdalosti,
				DokumentUrl = item.dokumentUrl,
				Oddil = item.oddil,
				Poznamka = item.poznamka,
			};
		}

		public static WsResult From(string item)
		{
			var parts = item.Split('#');
			if (parts.Length < 8)
			{
				return null;
			}

			var index = 0;
			return new WsResult
			{
				Id = Convert.ToInt64(parts[index++]),
				SpisovaZnacka = parts[index++],
				TypUdalosti = parts[index++],
				PopisUdalosti = parts[index++],
				DatumZalozeniUdalosti = DateTime.Parse(parts[index++]),
				DokumentUrl = parts[index++],
				Oddil = parts[index++],
				Poznamka = parts[index++]?.Replace("@<fl>", "\n")?.Replace("@<cr>", "\r"),
			};
		}

		public string ToStringLine()
		{
			return $"{Id}#{SpisovaZnacka}#{TypUdalosti}#{PopisUdalosti}#{DatumZalozeniUdalosti}#{DokumentUrl}#{Oddil}#{Poznamka?.Replace("\n", "@<fl>")?.Replace("\r", "@<cr>")}";
		}
	}
}
