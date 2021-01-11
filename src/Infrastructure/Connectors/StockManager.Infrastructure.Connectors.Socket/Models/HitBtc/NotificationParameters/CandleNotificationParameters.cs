using Newtonsoft.Json;
using StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.Market;

namespace StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.NotificationParameters
{
	class CandleNotificationParameters
	{
		[JsonProperty(PropertyName = "symbol")]
		public string CurrencyPairId { get; set; }

		[JsonProperty(PropertyName = "period")]
		public string Period { get; set; }
		
		[JsonProperty(PropertyName = "data")]
		public Candle[] Candles { get; set; }
	}
}
