using HlidacStatu.Api.Dataset.Connector;
using System;

namespace InsolvencniRejstrik.FromSearch
{
	public class Rizeni : IDatasetItem
	{
		public string Id { get; set; }
		public SenatniZnacka SpisovaZnacka { get; set; }
		public string Soud { get; set; }
		public DateTime ZahajeniRizeni { get; set; }
		public string Nazev { get; set; }
		public string ICO { get; set; }
		public string Rc { get; set; }
		public string RcBezLomitka { get; set; }
		public string AktualniStav { get; set; }
		public string Url { get; set; }
	}
}
