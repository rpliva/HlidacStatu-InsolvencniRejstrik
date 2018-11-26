using System;
using System.Collections.Generic;

namespace InsolvencniRejstrik.ByEvents
{
	class Stats
	{
		public int EventsCount { get; set; }
		public int RizeniCount { get; set; }
		public int DocumentCount { get; set; }
		public int LinkCount { get; set; }
		public int LinkCacheCount { get; set; }
		public int StateChangedCount { get; set; }
		public int NewOsobaCount { get; set; }
		public int OsobaChangedEvent { get; set; }
		public List<string> Errors { get; }
		public int TotalErrors { get; private set; }
		public long LastEventId { get; set; }
		public DateTime LastEventTime { get; set; }
		public DateTime Start { get; }
		public int PersonCacheGet { get; set; }
		public int PersonCacheSet { get; set; }
		public int DocumentCacheGet { get; set; }
		public int DocumentCacheSet { get; set; }
		public int InsolvencyProceedingCacheGet { get; set; }
		public int InsolvencyProceedingCacheSet { get; set; }
		public int PersonGet { get; set; }
		public int PersonSet { get; set; }
		public int DocumentGet { get; set; }
		public int DocumentSet { get; set; }
		public int InsolvencyProceedingGet { get; set; }
		public int InsolvencyProceedingSet { get; set; }

		public void WriteError(string message, long eventId)
		{
			if (Errors.Count > 10)
			{
				Errors.RemoveAt(Errors.Count - 1);
			}
			Errors.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message} ({eventId})");
			TotalErrors++;
		}

		public TimeSpan Duration() {
			return DateTime.Now - Start;
		}

		public string RunningTime()
		{
			var duration = Duration();
			return $"{duration.Hours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
		}

		public Stats()
		{
			Errors = new List<string>();
			Start = DateTime.Now;
		}
	}
}
