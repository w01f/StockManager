using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.HitBtc.Rest.Models.Market
{
	class CurrencyPair
	{
		[JsonProperty(PropertyName = "id")]
		public string Id { get; set; }

		[JsonProperty(PropertyName = "baseCurrency")]
		public string BaseCurrencyId { get; set; }

		[JsonProperty(PropertyName = "quoteCurrency")]
		public string QuoteCurrencyId { get; set; }

		[JsonProperty(PropertyName = "quantityIncrement")]
		public decimal QuantityIncrement { get; set; }

		[JsonProperty(PropertyName = "tickSize")]
		public decimal TickSize { get; set; }

		[JsonProperty(PropertyName = "takeLiquidityRate")]
		public decimal? TakeLiquidityRate { get; set; }

		[JsonProperty(PropertyName = "provideLiquidityRate")]
		public decimal? ProvideLiquidityRate { get; set; }

		[JsonProperty(PropertyName = "feeCurrency")]
		public string FeeCurrencyId { get; set; }
	}
}
