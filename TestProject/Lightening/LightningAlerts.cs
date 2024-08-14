using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Conn.RiskEventSource.StormGeo.Core.Lightning {
	public class LightningAlerts {
		[JsonProperty("alerts")]
		public List<LightningAttributes> LightningAttributes { get; set; }
	}
}
