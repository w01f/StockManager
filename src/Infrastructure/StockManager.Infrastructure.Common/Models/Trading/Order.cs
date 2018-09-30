using System;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Common.Models.Trading
{
	public class Order
	{
		public Int64 ExtId { get; set; }
		public Guid ClientId { get; set; }
		public Guid ParentClientId { get; set; }
		public CurrencyPair CurrencyPair { get; set; }
		public OrderRoleType Role { get; set; }
		public OrderSide OrderSide { get; set; }
		public OrderType OrderType { get; set; }
		public OrderStateType OrderStateType { get; set; }
		public OrderTimeInForceType TimeInForce { get; set; }
		public decimal Quantity { get; set; }
		public decimal Price { get; set; }
		public decimal? StopPrice { get; set; }
		public OrderAnalysisInfo AnalysisInfo { get; set; }
		public DateTime Created { get; set; }
		public DateTime Updated { get; set; }

		public Order Clone()
		{
			var newOrder = new Order();
			newOrder.ExtId = ExtId;
			newOrder.ClientId = ClientId;
			newOrder.ParentClientId = ParentClientId;
			newOrder.CurrencyPair = CurrencyPair;
			newOrder.Role = Role;
			newOrder.OrderSide = OrderSide;
			newOrder.OrderType = OrderType;
			newOrder.OrderStateType = OrderStateType;
			newOrder.TimeInForce = TimeInForce;
			newOrder.Quantity = Quantity;
			newOrder.Price = Price;
			newOrder.StopPrice = StopPrice;
			newOrder.AnalysisInfo = AnalysisInfo;
			newOrder.Created = Created;
			newOrder.Updated = Updated;
			return newOrder;
		}
	}
}
