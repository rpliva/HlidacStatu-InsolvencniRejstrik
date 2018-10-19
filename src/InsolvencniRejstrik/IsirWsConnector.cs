using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace InsolvencniRejstrik
{
	class IsirWsConnector
	{
		private IsirWs.IsirWsPublicPortTypeClient Client = new IsirWs.IsirWsPublicPortTypeClient();

		public async Task Handle(long id)
		{
			await HandleInner(id, new Dictionary<string, Rizeni>());
		}

		private async Task HandleInner(long id, Dictionary<string, Rizeni> listOfProceedings)
		{
			var response = await Client.getIsirWsPublicPodnetIdAsync(new IsirWs.getIsirWsPublicPodnetIdRequest { idPodnetu = id });

			if (response.status.stav == IsirWs.stavType.OK)
			{
				foreach (var item in response.data)
				{
					Rizeni rizeni;
					if (!listOfProceedings.TryGetValue(item.spisovaZnacka, out rizeni))
					{
						rizeni = new Rizeni { SpisovaZnacka = item.spisovaZnacka };
						listOfProceedings.Add(rizeni.SpisovaZnacka, rizeni);
					}

					if (!string.IsNullOrEmpty(item.dokumentUrl))
					{
						rizeni.Dokumenty.Add(new Dokument { Url = item.dokumentUrl, DatumVlozeni = item.datumZalozeniUdalosti, Popis = item.popisUdalosti });
					}

					if (!string.IsNullOrEmpty(item.poznamka)) {
						var xdoc = XDocument.Parse(item.poznamka);
						switch (item.typUdalosti)
						{
							case "1":
								try
								{
									var idPuvodce = parseValue(xdoc, "//idOsobyPuvodce");
									var osobaId = parseValue(xdoc, "//osoba/idOsoby");
									var key = $"{idPuvodce}-{osobaId}";
									Osoba osoba;
									if (!rizeni.Osoby.TryGetValue(key, out osoba))
									{
										osoba = new Osoba { IdPuvodce = idPuvodce, Id = osobaId };
										rizeni.Osoby.Add(key, osoba);
									}

									osoba.Typ = parseValue(xdoc, "//osoba/druhOsoby");
									osoba.Role = parseValue(xdoc, "//osoba/druhRoleVRizeni");

									if (osoba.Typ == "P") {
										osoba.Nazev = parseValue(xdoc, "//osoba/nazevOsoby");
										osoba.ICO = parseValue(xdoc, "//osoba/ic");
									}
									else if (osoba.Typ == "F") {
										osoba.Nazev = string.Join(" ", new[] {
											parseValue(xdoc, "//osoba/jmeno"),
											parseValue(xdoc, "//osoba/nazevOsoby"),
										}.Where(i => !string.IsNullOrEmpty(i)));
										osoba.Rc = parseValue(xdoc, "//osoba/rc");
										var date = parseValue(xdoc, "//osoba/datumNarozeni");
										if (!string.IsNullOrEmpty(date)) {
											osoba.DatumNarozeni = DateTime.Parse(date);
										}
									}
									else {
										throw new ApplicationException($"Unknown type of Osoba - {osoba.Typ}");
									}

									Console.WriteLine(osoba);
								}
								catch (Exception e)
								{

									Console.WriteLine(e.Message);
								}


								break;
							case "2":
								// <?xml version="1.0" encoding="UTF-8" standalone="yes"?><ns2:udalost xmlns:ns2="http://www.cca.cz/isir/poznamka" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" verzeXsd="1" xsi:schemaLocation="https://isir.justice.cz:8443/isir_public_ws/xsd/poznamka.xsd"><idOsobyPuvodce>KSZPCPM</idOsobyPuvodce><osoba><idOsoby>MILPA A.S. V49240072</idOsoby><druhRoleVRizeni>DLUŽNÍK</druhRoleVRizeni><nazevOsoby>Milpa a.s. v likvidaci</nazevOsoby><druhOsoby>P</druhOsoby><ic>49240072</ic><adresa><druhAdresy>SÍDLO ORG.</druhAdresy><datumPobytOd>2008-01-03+01:00</datumPobytOd><mesto>Plzeň</mesto><ulice>Zábělská</ulice><cisloPopisne>19</cisloPopisne><psc>312 00</psc><idAdresy>119943</idAdresy></adresa></osoba></ns2:udalost>

								// <?xml version="1.0" encoding="UTF-8" standalone="yes"?><ns2:udalost xmlns:ns2="http://www.cca.cz/isir/poznamka" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" verzeXsd="1" xsi:schemaLocation="https://isir.justice.cz:8443/isir_public_ws/xsd/poznamka.xsd"><idOsobyPuvodce>KSZPCPM</idOsobyPuvodce><osoba><idOsoby>"GRAL" SPOL.40525970</idOsoby><druhRoleVRizeni>DLUŽNÍK</druhRoleVRizeni><nazevOsoby>"GRAL" spol. s r.o. v likvidaci</nazevOsoby><druhOsoby>P</druhOsoby><ic>40525970</ic><adresa><druhAdresy>SÍDLO ORG.</druhAdresy><datumPobytOd>2008-01-03+01:00</datumPobytOd><mesto>Plzeň</mesto><ulice>Křimická</ulice><cisloPopisne>48</cisloPopisne><psc>318 01</psc><idAdresy>119963</idAdresy></adresa></osoba></ns2:udalost>

								// <?xml version="1.0" encoding="UTF-8" standalone="yes"?><ns2:udalost xmlns:ns2="http://www.cca.cz/isir/poznamka" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" verzeXsd="1" xsi:schemaLocation="https://isir.justice.cz:8443/isir_public_ws/xsd/poznamka.xsd"><idOsobyPuvodce>KSZPCPM</idOsobyPuvodce><osoba><idOsoby>LEPŠÍ  LUDĚ080654  1</idOsoby><druhRoleVRizeni>DLUŽNÍK</druhRoleVRizeni><nazevOsoby>Lepší</nazevOsoby><druhOsoby>F</druhOsoby><jmeno>Luděk</jmeno><rc>540608/1197</rc><adresa><druhAdresy>TRVALÁ</druhAdresy><datumPobytOd>2008-01-03+01:00</datumPobytOd><mesto>Lučice 27</mesto><psc>339 01</psc><idAdresy>119967</idAdresy></adresa></osoba></ns2:udalost>
								Console.WriteLine($"Zmena adresy osoby { item.spisovaZnacka}");
								break;
							case "5":
								rizeni.Stav = xdoc.XPathSelectElement("//vec/druhStavRizeni").Value;
								break;

						}
					}
				}
			}
			else
			{
				Console.WriteLine($"Id posledniho zaznamu: {id}");
				Console.WriteLine();
				Console.WriteLine(response.status.kodChyby);
				Console.WriteLine(response.status.popisChyby);

				return;
			}

			await HandleInner(response.data.Last().id, listOfProceedings);
		}

		private string parseValue(XDocument xdoc, string xpath) {
			return xdoc.XPathSelectElement("//osoba/rc")?.Value ?? "";
		}

		class Rizeni
		{
			public string SpisovaZnacka { get; set; }
			public string Stav { get; set; }
			public Dictionary<string, Osoba> Osoby { get; set; }
			public List<Dokument> Dokumenty { get; set; }

			public Rizeni()
			{
				Osoby = new Dictionary<string, Osoba>();
				Dokumenty = new List<Dokument>();
			}

			public override string ToString() => $"{SpisovaZnacka} - {Stav} (O: {Osoby.Count}, D: {Dokumenty.Count})";
		}

		public class Osoba
		{
			public string IdPuvodce { get; set; }
			public string Id { get; set; }
			public string Nazev { get; set; }
			public string Role { get; set; }
			public string Typ { get; set; }
			public string ICO { get; set; }
			public string Rc { get; set; }
			public DateTime? DatumNarozeni { get; set; }

			public override string ToString() => Typ == "P" 
				? $"{IdPuvodce}: {Nazev} - {Role} (ic: {ICO})"
				: $"{IdPuvodce}: {Nazev} - {Role} (rc: {Rc}, {(DatumNarozeni.HasValue ? DatumNarozeni.Value.ToShortDateString() : "-")})";
		}

		class Dokument
		{
			public DateTime DatumVlozeni { get; set; }
			public string Popis { get; set; }
			public string Url { get; set; }
			public string Oddil { get; set; }

			public override string ToString() => $"{DatumVlozeni:dd.MM.yyyy} ({Oddil}) - {Popis}";
		}
	}
}
