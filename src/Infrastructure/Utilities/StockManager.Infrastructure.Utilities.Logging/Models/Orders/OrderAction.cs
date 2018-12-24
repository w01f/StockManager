using System;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Utilities.Logging.Common.Enums;

namespace StockManager.Infrastructure.Utilities.Logging.Models.Orders
{
	public class OrderAction : BaseLogAction
	{
		public override LogActionType LogActionType => LogActionType.OrderAction;
		public OrderActionType OrderActionType { get; set; }
		public Guid ClinetId { get; set; }
		public Guid ParentClientId { get; set; }
		public string CurrencyPair { get; set; }
		public OrderRoleType Role { get; set; }
		public OrderSide OrderSide { get; set; }
		public OrderType OrderType { get; set; }
		public OrderStateType OrderStateType { get; set; }
		public OrderTimeInForceType TimeInForce { get; set; }
		public decimal Quantity { get; set; }
		public decimal Price { get; set; }
		public decimal? StopPrice { get; set; }
	}
}
