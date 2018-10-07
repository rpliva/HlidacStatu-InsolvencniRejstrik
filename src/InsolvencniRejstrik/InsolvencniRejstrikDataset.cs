namespace InsolvencniRejstrik
{
	public static class InsolvencniRejstrikDataset
	{
		private static Template SearchResultTemplate = new Template
		{
			Header = @"<table class=""table table-hover"">
    <thead>
        <tr>
            <th>Spisová značka</th>
            <th>Soud</th>
            <th>Datum zahájení řízení</th>
            <th>Jméno/název</th>
            <th>Aktualní stav</th>
        </tr>
    </thead>
    <tbody>",
			Body = @"
        <tr>
            <td style=""white-space: nowrap;"">
                <a href=""@(fn_DatasetItemUrl(item.Id))"">@item.SpisovaZnacka.SoudniOddeleni @item.SpisovaZnacka.RejstrikovaZnacka @item.SpisovaZnacka.Cislo / @item.SpisovaZnacka.Rocnik</a>
            </td>
            <td style=""white-space: nowrap;"">
                @item.Soud
            </td>
            <td>
                @(fn_FormatDate(item.ZahajeniRizeni, ""dd.MM.yyyy HH:mm""))
            </td>
            <td style=""white-space: nowrap;"">
                @if(!fn_IsNullOrEmpty(item.ICO))
                {
                    <a href=""https://www.hlidacstatu.cz/subjekt/@(item.ICO)"">@fn_NormalizeText(item.Nazev)</a><br />
                }
				else if(!fn_IsNullOrEmpty(item.RcBezLomitka))
                {
                    <a href=""https://www.hlidacstatu.cz/subjekt/@(item.RcBezLomitka)"">@fn_NormalizeText(item.Nazev)</a><br />
                }
				else
                {
                    @fn_NormalizeText(item.Nazev)
                }
            </td>
            <td style=""white-space: nowrap;"">
                @item.AktualniStav
            </td>
        </tr>",
			Footer = @"</tbody></table>"
		};

		private static Template DetailTemplate = new Template {
			Header = "<h3>@item.Nazev</h3>",
			Body = @"<table class=""table table-hover"">
        <tbody>
            <tr>
                <td>Spisová značka</td>
                <td>@item.SpisovaZnacka.SoudniOddeleni @item.SpisovaZnacka.RejstrikovaZnacka @item.SpisovaZnacka.Cislo / @item.SpisovaZnacka.Rocnik</td>
            </tr>
            <tr>
                <td>Soud</td>
                <td>@item.Soud</td>
            </tr>
            <tr>
                <td>Datum zahájení řízení</td>
                <td>@(fn_FormatDate(item.ZahajeniRizeni, ""dd.MM.yyyy HH:mm""))</td>
            </tr>
			@if (!fn_IsNullOrEmpty(item.AktualniStav))
			{
				<tr>
					<td>Aktuální stav</td>
					<td>@item.AktualniStav</td>
				</tr>
			}
            <tr>
                <td>Jméno/název</td>
                <td>
					@if(!fn_IsNullOrEmpty(item.ICO))
					{
						<a href=""https://www.hlidacstatu.cz/subjekt/@(item.ICO)"">@fn_NormalizeText(item.Nazev)</a><br />
					}
					else if(!fn_IsNullOrEmpty(item.RcBezLomitka))
					{
						<a href=""https://www.hlidacstatu.cz/subjekt/@(item.RcBezLomitka)"">@fn_NormalizeText(item.Nazev)</a><br />
					}
					else
					{
						@fn_NormalizeText(item.Nazev)
					}
				</td>
            </tr>
			@if (!fn_IsNullOrEmpty(item.Rc))
			{
				<tr>
					<td>Rodné číslo</td>
					<td>@item.Rc</td>
				</tr>
			}
			@if (!fn_IsNullOrEmpty(item.ICO))
			{
				<tr>
					<td>IČ</td>
					<td>@item.ICO</td>
				</tr>
			}
			@if (!fn_IsNullOrEmpty(item.Url))
			{
				<tr>
					<td>Zdroj na ISIR</td>
					<td><a href=""@item.Url"" target=""_blank"">@item.Url</a></td>
				</tr>
			}
        </tbody>
    </table>"
		};

		public static Dataset<Rizeni> InsolvencniRejstrik = new Dataset<Rizeni>(
			name: "Insolvenční rejstřík",
			datasetId: "insolvencni-rejstrik",
			origUrl: "https://isir.justice.cz/isir/common/index.do",
			description: "Seznam dlužníků vedený v insolvenčním rejstříku, proti kterým bylo zahájeno insolvenční řízení po 1. lednu 2008 a nebyli z rejstříku vyškrtnuti dle § 425 insolvenčního zákona.",
			sourceCodeUrl: "https://github.com/rpliva/HlidacStatu-InsolvencniRejstrik",
			orderList: new string[,] { 
				{ "Soud", "Soud" }, 
				{ "Datum zahájení řízení", "ZahajeniRizeni" }, 
				{ "Jméno/název", "Nazev" }, 
				{ "Aktualní stav", "AktualniStav" }
			},
			betaVersion: false,
			searchResultTemplate: SearchResultTemplate,
			detailTemplate: DetailTemplate);
	}
}
