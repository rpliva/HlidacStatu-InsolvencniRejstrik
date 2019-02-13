using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace InsolvencniRejstrik.ByEvents
{
	public class FileRepository : IRepository
	{
		private ConcurrentDictionary<string, Rizeni> RizeniCache = new ConcurrentDictionary<string, Rizeni>();

		public Rizeni GetInsolvencyProceeding(string spisovaZnacka, Func<string, Rizeni> createNewInsolvencyProceeding)
		{
			return RizeniCache.GetOrAdd(spisovaZnacka, createNewInsolvencyProceeding(spisovaZnacka));
		}

		public void SetInsolvencyProceeding(Rizeni item)
		{
			var dir = $@"data\{item.SpisovaZnacka.Split('/')[1]}";

			try
			{
				File.WriteAllText($@"{dir}\{item.UrlId()}.json", JsonConvert.SerializeObject(item, Formatting.Indented));
			}
			catch (DirectoryNotFoundException)
			{
				Directory.CreateDirectory(dir);
				File.WriteAllText($@"{dir}\{item.UrlId()}.json", JsonConvert.SerializeObject(item, Formatting.Indented));
			}
		}
	}
}
