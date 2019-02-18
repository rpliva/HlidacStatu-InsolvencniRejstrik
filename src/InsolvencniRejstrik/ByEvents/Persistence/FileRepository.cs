using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace InsolvencniRejstrik.ByEvents
{
	public class FileRepository : IRepository
	{
		private ConcurrentDictionary<string, Rizeni> RizeniCache = new ConcurrentDictionary<string, Rizeni>();
		private bool IgnoreCache;

		public FileRepository(bool ignoreCache)
		{
			IgnoreCache = ignoreCache;
		}

		public Rizeni GetInsolvencyProceeding(string spisovaZnacka, Func<string, Rizeni> createNewInsolvencyProceeding)
		{
			Rizeni rizeni = null;
			if (!IgnoreCache && RizeniCache.TryGetValue(spisovaZnacka, out rizeni) && rizeni != null)
			{
				return rizeni;
			}

			var noveRizeni = createNewInsolvencyProceeding(spisovaZnacka);
			var filePath = GetFilePath(noveRizeni);

			if (File.Exists(filePath.FullPath))
			{
				rizeni = JsonConvert.DeserializeObject<Rizeni>(File.ReadAllText(filePath.FullPath));
				if (!IgnoreCache && !RizeniCache.TryAdd(spisovaZnacka, rizeni))
				{
					throw new ApplicationException($"Rizeni {spisovaZnacka} already exists in cache");
				}
				return rizeni;
			}

			if (!IgnoreCache && !RizeniCache.TryAdd(spisovaZnacka, noveRizeni))
			{
				throw new ApplicationException($"New rizeni {spisovaZnacka} already exists in cache");
			}
			return noveRizeni;
		}

		public void SetInsolvencyProceeding(Rizeni item)
		{
			var filePath = GetFilePath(item);

			try
			{
				File.WriteAllText(filePath.FullPath, JsonConvert.SerializeObject(item, Formatting.Indented));
			}
			catch (DirectoryNotFoundException)
			{
				Directory.CreateDirectory(filePath.Dir);
				File.WriteAllText(filePath.FullPath, JsonConvert.SerializeObject(item, Formatting.Indented));
			}
		}

		private FilePath GetFilePath(Rizeni item)
		{
			var dir = $@"data\{item.SpisovaZnacka.Split('/')[1]}";
			return new FilePath { FullPath = $@"{dir}\{item.UrlId()}.json", Dir = dir };
		}

		private class FilePath
		{
			public string Dir { get; set; }
			public string FullPath { get; set; }
		}
	}
}
