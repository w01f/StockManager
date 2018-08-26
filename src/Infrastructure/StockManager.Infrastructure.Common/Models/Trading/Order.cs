using System;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Common.Models.Trading
{
	public class Order
	{
		public Int64 ExtId { get; set; }
		public Guid ClientId { get; set; }
		public Guid ParentClientId { get; set; }
		public CurrencyPair CurrencyPair { get; set; }
		public OrderSide OrderSide { get; set; }
		public OrderType OrderType { get; set; }
		public OrderStateType OrderStateType { get; set; }
		public decimal Quantity { get; set; }
		public decimal Price { get; set; }
		public decimal? StopPrice { get; set; }
		public DateTime Created { get; set; }
		public DateTime Updated { get; set; }
	}
}
