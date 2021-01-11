using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.Market
{
	class OrderBookItem
	{
		[JsonProperty(PropertyName = "price")]
		public decimal Price { get; set; }

		[JsonProperty(PropertyName = "size")]
		public decimal Size { get; set; }
	}
}
