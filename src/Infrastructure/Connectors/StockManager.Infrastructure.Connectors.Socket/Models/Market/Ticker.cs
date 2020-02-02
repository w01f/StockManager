using System;
using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Models.Market
{
	public class Ticker
	{
		[JsonProperty(PropertyName = "symbol")]
		public string CurrencyPairId { get; set; }

		[JsonProperty(PropertyName = "ask")]
		public decimal? BestAskPrice { get; set; }

		[JsonProperty(PropertyName = "bid")]
		public decimal? BestBidPrice { get; set; }

		[JsonProperty(PropertyName = "last")]
		public decimal? LastPrice { get; set; }

		[JsonProperty(PropertyName = "open")]
		public decimal? OpenPrice { get; set; }

		[JsonProperty(PropertyName = "low")]
		public decimal? MinPrice { get; set; }

		[JsonProperty(PropertyName = "high")]
		public decimal? MaxPrice { get; set; }

		[JsonProperty(PropertyName = "volume")]
		public decimal? VolumeInBaseCurrency { get; set; }

		[JsonProperty(PropertyName = "volumeQuote")]
		public decimal? VolumeInQuoteCurrency { get; set; }

		[JsonProperty(PropertyName = "timestamp")]
		public DateTime? Updated { get; set; }
	}
}
