using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Rest.Models.Trading
{
	public class TradingBallance
	{
		[JsonProperty(PropertyName = "currency")]
		public string CurrencyId { get; set; }

		[JsonProperty(PropertyName = "available")]
		public decimal Available { get; set; }

		[JsonProperty(PropertyName = "reserved")]
		public decimal Reserved { get; set; }
	}
}
