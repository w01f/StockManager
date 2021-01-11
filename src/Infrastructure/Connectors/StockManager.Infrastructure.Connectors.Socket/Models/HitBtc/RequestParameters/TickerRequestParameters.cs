using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.RequestParameters
{
	class TickerRequestParameters
	{
		[JsonProperty(PropertyName = "symbol")]
		public string CurrencyPairId { get; set; }
	}
}
