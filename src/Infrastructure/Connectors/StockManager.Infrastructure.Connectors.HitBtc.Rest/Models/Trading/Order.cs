using System;
using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.HitBtc.Rest.Models.Trading
{
	public class Order
	{
		[JsonProperty(PropertyName = "id")]
		public decimal Id { get; set; }

		[JsonProperty(PropertyName = "clientOrderId")]
		public string ClientId { get; set; }

		[JsonProperty(PropertyName = "symbol")]
		public string CurrencyPairId { get; set; }

		[JsonProperty(PropertyName = "side")]
		public string OrderSide { get; set; }

		[JsonProperty(PropertyName = "status")]
		public string OrderStateType { get; set; }

		[JsonProperty(PropertyName = "type")]
		public string OrderType { get; set; }

		[JsonProperty(PropertyName = "timeInForce")]
		public string TimeInForce { get; set; }

		[JsonProperty(PropertyName = "quantity")]
		public decimal Quantity { get; set; }

		[JsonProperty(PropertyName = "price")]
		public decimal Price { get; set; }

		[JsonProperty(PropertyName = "cumQuantity")]
		public decimal TotalPrice { get; set; }

		[JsonProperty(PropertyName = "createdAt")]
		public DateTime Created { get; set; }

		[JsonProperty(PropertyName = "updatedAt")]
		public DateTime Updated { get; set; }

		[JsonProperty(PropertyName = "stopPrice")]
		public decimal StopPrice { get; set; }

		[JsonProperty(PropertyName = "postOnly")]
		public bool PostOnly { get; set; }

		[JsonProperty(PropertyName = "expireTime")]
		public DateTime ExpireTime { get; set; }
	}
}
