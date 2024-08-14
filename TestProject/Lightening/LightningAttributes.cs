using System;
//using IDV.VCC.Data.Spatial;
using Newtonsoft.Json;

namespace Conn.RiskEventSource.StormGeo.Core.Lightning {
	public class LightningAttributes {

		[JsonProperty("pointId")]
		public string PointId { get; set; }

		[JsonProperty("pointName")]
		public string Title { get; set; }

		[JsonProperty("pointLat")]
		public string Latitude { get; set; }

		[JsonProperty("pointLon")]
		public string Longitude { get; set; }

		public string Description => "Lightning strike near " + Title;

		public Alert Alert { get; set; }

		/// <summary>
		/// Convert this <see cref= "LocationPoint" /> to a VCC <see cref = "Geometry" />.
		/// </ summary >
		/// < returns >
		/// A < see cref= "Geometry" />.May be<see cref="Geometry.EmptyPoint"/> if either<see cref="Latitude"/> or
		/// <see cref = "Longitude" /> are missing.
		/// </returns>
		//public Geometry ToGeometry()
		//{
		//	if (!string.IsNullOrWhiteSpace(Latitude) && !string.IsNullOrWhiteSpace(Longitude))
		//	{
		//		return new Geometry($"POINT ({Longitude} {Latitude})");
		//	}
		//	return Geometry.EmptyPoint;
		//}
	}
}
