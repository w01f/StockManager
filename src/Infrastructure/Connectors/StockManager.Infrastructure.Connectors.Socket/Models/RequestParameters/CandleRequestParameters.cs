using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Models.RequestParameters
{
	class CandleRequestParameters
	{
		[JsonProperty(PropertyName = "symbol")]
		public string CurrencyPairId { get; set; }

		[JsonProperty(PropertyName = "period")]
		public string Period { get; set; }

		[JsonProperty(PropertyName = "limit")]
		public int Limit { get; set; }
	}
}
