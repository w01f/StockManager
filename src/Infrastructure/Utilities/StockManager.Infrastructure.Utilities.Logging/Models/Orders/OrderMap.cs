﻿using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Utilities.Logging.Common.Enums;

namespace StockManager.Infrastructure.Utilities.Logging.Models.Orders
{
	public static class OrderMap
	{
		public static OrderAction ToLogAction(this Order source, OrderActionType actionType)
		{
			var action  = new OrderAction();
			action.OrderActionType = actionType;

			action.ClinetId = source.ClientId;
			action.ParentClientId = source.ParentClientId;
			action.CurrencyPair = source.CurrencyPair.Id;
			action.Role = source.Role;
			action.OrderSide = source.OrderSide;
			action.OrderType = source.OrderType;
			action.OrderStateType = source.OrderStateType;
			action.TimeInForce = source.TimeInForce;
			action.Quantity = source.Quantity;
			action.Price = source.Price;
			action.StopPrice = source.StopPrice;

			return action;
		}
	}
}
