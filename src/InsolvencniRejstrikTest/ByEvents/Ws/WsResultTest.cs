using System;
using Xunit;

namespace InsolvencniRejstrik.ByEvents
{
	public class WsResultTest
	{
		[Fact]
		public void ToStringLineAndThenParse() {
			var expected = new WsResult {
				Id = 123,
				DatumZalozeniUdalosti = new DateTime(2018, 12, 09),
				DokumentUrl = "http://document.url/id=9999",
				Oddil = "A",
				PopisUdalosti = "popis udalosti",
				SpisovaZnacka = "ABC 123-AB-4567",
				TypUdalosti = "typ",
				Poznamka = "<xml><element>poznamka</element></xml>"
			};

			var actual = WsResult.From(expected.ToStringLine());

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ParseWithHashInName() {
			var expected = new WsResult
			{
				Id = 123,
				DatumZalozeniUdalosti = new DateTime(2018, 12, 09),
				DokumentUrl = "http://document.url/id=9999",
				Oddil = "A",
				PopisUdalosti = "popis udalosti",
				SpisovaZnacka = "ABC 123-AB-4567",
				TypUdalosti = "typ",
				Poznamka = "<xml><osoba><nazevOsoby>Zollner Weberei-W#schefabrik GmbH + Co.KG</nazevOsoby></osoba></xml>"
			};

			var actual = WsResult.From(expected.ToStringLine());

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void RealRecordWithHash() {
			var stringLine = "8840910#INS 11459/2013#625#Zasílání údajů o změně osoby věřitele v přihlášce#30.09.2013 12:01:51###<?xml version=\"1.0\" encoding=\"UTF-8\" standalone =\"yes\" ?><ns2:udalost xmlns:ns2=\"http://www.cca.cz/isir/poznamka\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" verzeXsd =\"1_9\" xsi:schemaLocation=\"https://isir.justice.cz:8443/isir_public_ws/xsd/poznamka_1_9.xsd\" ><idOsobyPuvodce>KSJIMBM</idOsobyPuvodce><osoba><idOsoby>ZOLLNER W          1</idOsoby><druhRoleVRizeni>VĚŘITEL</druhRoleVRizeni><nazevOsoby>Zollner Weberei-W#schefabrik GmbH + Co.KG</nazevOsoby><druhOsoby>P</druhOsoby></osoba><priznakAnVedlejsiUdalost>F</priznakAnVedlejsiUdalost><priznakAnVedlejsiDokument>F</priznakAnVedlejsiDokument><druhOddilPrihl>P23</druhOddilPrihl><cisloOddiluPrihl>1</cisloOddiluPrihl><osobaVeritel>ZOLLNER W          1</osobaVeritel><priznakPlatnyVeritel>T</priznakPlatnyVeritel><priznakMylnyZapisVeritelPohled>F</priznakMylnyZapisVeritelPohled></ns2:udalost>";
			var expectedPoznamka = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone =\"yes\" ?><ns2:udalost xmlns:ns2=\"http://www.cca.cz/isir/poznamka\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" verzeXsd =\"1_9\" xsi:schemaLocation=\"https://isir.justice.cz:8443/isir_public_ws/xsd/poznamka_1_9.xsd\" ><idOsobyPuvodce>KSJIMBM</idOsobyPuvodce><osoba><idOsoby>ZOLLNER W          1</idOsoby><druhRoleVRizeni>VĚŘITEL</druhRoleVRizeni><nazevOsoby>Zollner Weberei-W#schefabrik GmbH + Co.KG</nazevOsoby><druhOsoby>P</druhOsoby></osoba><priznakAnVedlejsiUdalost>F</priznakAnVedlejsiUdalost><priznakAnVedlejsiDokument>F</priznakAnVedlejsiDokument><druhOddilPrihl>P23</druhOddilPrihl><cisloOddiluPrihl>1</cisloOddiluPrihl><osobaVeritel>ZOLLNER W          1</osobaVeritel><priznakPlatnyVeritel>T</priznakPlatnyVeritel><priznakMylnyZapisVeritelPohled>F</priznakMylnyZapisVeritelPohled></ns2:udalost>";

			var actual = WsResult.From(stringLine);

			Assert.Equal(8840910, actual.Id);
			Assert.Equal(expectedPoznamka, actual.Poznamka);
		}
	}
}
