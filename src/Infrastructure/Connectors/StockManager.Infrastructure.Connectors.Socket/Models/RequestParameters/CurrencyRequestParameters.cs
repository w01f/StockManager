using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Models.RequestParameters
{
	class CurrencyRequestParameters
	{
		[JsonProperty(PropertyName = "symbol")]
		public string CurrencyId { get; set; }
	}
}
