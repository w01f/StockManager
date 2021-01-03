using System;
using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Models.RequestParameters
{
	class CreateOrderRequestParameters
	{
		[JsonProperty(PropertyName = "clientOrderId")]
		public string ClientId { get; set; }

		[JsonProperty(PropertyName = "symbol")]
		public string CurrencyPairId { get; set; }

		[JsonProperty(PropertyName = "side")]
		public string OrderSide { get; set; }

		[JsonProperty(PropertyName = "type")]
		public string OrderType { get; set; }

		[JsonProperty(PropertyName = "timeInForce")]
		public string TimeInForce { get; set; }

		[JsonProperty(PropertyName = "quantity")]
		public decimal Quantity { get; set; }

		[JsonProperty(PropertyName = "price")]
		public decimal Price { get; set; }

		[JsonProperty(PropertyName = "stopPrice")]
		public decimal StopPrice { get; set; }

		[JsonProperty(PropertyName = "postOnly")]
		public bool PostOnly { get; set; }

		[JsonProperty(PropertyName = "expireTime")]
		public DateTime ExpireTime { get; set; }

		[JsonProperty(PropertyName = "strictValidate")]
		public bool StrictValidate { get; set; }
	}
}
