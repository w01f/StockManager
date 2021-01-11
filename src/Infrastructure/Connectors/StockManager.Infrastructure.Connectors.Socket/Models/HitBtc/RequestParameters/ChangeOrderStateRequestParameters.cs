using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.RequestParameters
{
	class ChangeOrderStateRequestParameters
	{
		[JsonProperty(PropertyName = "clientOrderId")]
		public string ExistingClientId { get; set; }

		[JsonProperty(PropertyName = "requestClientId")]
		public string NewClientId { get; set; }

		[JsonProperty(PropertyName = "quantity")]
		public decimal Quantity { get; set; }

		[JsonProperty(PropertyName = "price")]
		public decimal Price { get; set; }

		[JsonProperty(PropertyName = "stopPrice")]
		public decimal StopPrice { get; set; }
	}
}
