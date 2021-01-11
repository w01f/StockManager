using Newtonsoft.Json;
using StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.Market;

namespace StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.NotificationParameters
{
	class OrderBookNotificationParameters
	{
		[JsonProperty(PropertyName = "symbol")]
		public string CurrencyPairId { get; set; }

		[JsonProperty(PropertyName = "ask")]
		public OrderBookItem[] AskItems { get; set; }

		[JsonProperty(PropertyName = "bid")]
		public OrderBookItem[] BidItems { get; set; }
	}
}
