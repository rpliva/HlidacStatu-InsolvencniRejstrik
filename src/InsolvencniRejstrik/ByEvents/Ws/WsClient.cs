using System.Collections.Generic;
using System.Linq;

namespace InsolvencniRejstrik.ByEvents
{
	class WsClient : IWsClient
	{
		private IsirWs.IsirWsPublicPortTypeClient Client = new IsirWs.IsirWsPublicPortTypeClient();

		public IEnumerable<WsResult> Get(long id)
		{
			var latestId = id;
			IsirWs.getIsirWsPublicPodnetIdResponse response;
			do
			{
				response = Client.getIsirWsPublicPodnetIdAsync(new IsirWs.getIsirWsPublicPodnetIdRequest { idPodnetu = latestId }).Result;
				if (response.status.stav == IsirWs.stavType.OK)
				{
					foreach (var item in response.data)
					{
						yield return WsResult.From(item);
						latestId = item.id;
					}
				}
			} while (response.status.stav == IsirWs.stavType.OK && response.data.Count() > 0);
		}
	}
}
