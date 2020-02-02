using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Models.NotificationParameters
{
	class CandleNotificationParameters
	{
		[JsonProperty(PropertyName = "symbol")]
		public string CurrencyPairId { get; set; }

		[JsonProperty(PropertyName = "period")]
		public string Period { get; set; }
		
		[JsonProperty(PropertyName = "data")]
		public Market.Candle[] Candles { get; set; }
	}
}
