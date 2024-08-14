using System;
using Newtonsoft.Json;

namespace Conn.RiskEventSource.StormGeo.Core.Lightning {
	public class Alert {
		private DateTimeOffset _endTime;

		[JsonProperty("status")]
		public string Status { get; set; }

		[JsonProperty("sendTime")]
		public DateTime SendTime { get; set; }

		[JsonProperty("perimeterRadiusKm")]
		public int BufferRadiusInKm { get; set; }

		[JsonProperty("strikeTime")]
		public DateTime StartTime { get; set; }

		[JsonProperty("strikeLat")]
		public double? StrikeLatitude { get; set; }

		[JsonProperty("strikeLon")]
		public double? StrikeLongitude { get; set; }

		public DateTimeOffset EndTime {
			get {
				DateTimeOffset.TryParse(StartTime.ToString(), out var parsedResult);
				return Status.ToLowerInvariant() == "clear" ? DateTimeOffset.UtcNow : parsedResult.AddHours(24);
			}
			set { _endTime = value; }
		}
	}
}
