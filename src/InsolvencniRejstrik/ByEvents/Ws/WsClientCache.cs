using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace InsolvencniRejstrik.ByEvents
{
	class WsClientCache : IWsClient
	{

		private readonly Lazy<IWsClient> UnderlyingClient;
		private const string CacheFile = "ws_client_cache.csv";

		public WsClientCache(Lazy<IWsClient> underlyingClient)
		{
			UnderlyingClient = underlyingClient;
		}

		public IEnumerable<WsResult> Get(long id)
		{
			var latestId = id;
			if (File.Exists(CacheFile))
			{
				foreach (var item in File.ReadLines(CacheFile).Select(l => WsResult.From(l)).Where(r => r != null && r.Id >= id))
				{
					yield return item;
					latestId = item.Id;
				}
			}

			foreach (var item in UnderlyingClient.Value.Get(latestId))
			{
				File.AppendAllLines(CacheFile, new[] { item.ToStringLine() });
				yield return item;
			}
		}
	}
}
