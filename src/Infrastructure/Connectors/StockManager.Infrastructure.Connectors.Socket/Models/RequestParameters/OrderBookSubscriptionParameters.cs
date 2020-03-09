using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Models.RequestParameters
{
	class OrderBookSubscriptionParameters
	{
		[JsonProperty(PropertyName = "symbol")]
		public string CurrencyPairId { get; set; }
	}
}
