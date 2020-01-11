using System;
using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Rest.Models.Market
{
	public class Candle
	{
		[JsonProperty(PropertyName = "timestamp")]
		public DateTime Timestamp { get; set; }

		[JsonProperty(PropertyName = "open")]
		public decimal Open { get; set; }

		[JsonProperty(PropertyName = "close")]
		public decimal Close { get; set; }

		[JsonProperty(PropertyName = "max")]
		public decimal Max { get; set; }

		[JsonProperty(PropertyName = "min")]
		public decimal Min { get; set; }

		[JsonProperty(PropertyName = "volume")]
		public decimal Volume { get; set; }

		[JsonProperty(PropertyName = "volumeQuote")]
		public decimal VolumeQuote { get; set; }
	}
}
