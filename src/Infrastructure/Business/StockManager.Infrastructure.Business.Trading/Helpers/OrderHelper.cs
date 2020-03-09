using System;
using System.Collections.Generic;
using System.Linq;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Utilities.Configuration.Models;

namespace StockManager.Infrastructure.Business.Trading.Helpers
{
	static class OrderHelper
	{
		public static void CalculateBuyOrderQuantity(this Order order, TradingBallance tradingBalance, TradingSettings settings)
		{
			order.Quantity = Math.Floor((tradingBalance.Available * (1 - order.CurrencyPair.TakeLiquidityRate) * settings.MaxOrderUsingBalancePart / order.Price) / order.CurrencyPair.QuantityIncrement) * order.CurrencyPair.QuantityIncrement;
		}

		public static void CalculateSellOrderQuantity(this Order order, TradingBallance tradingBalance)
		{
			order.Quantity = Math.Floor(tradingBalance.Available * (1 - order.CurrencyPair.TakeLiquidityRate) / order.CurrencyPair.QuantityIncrement) * order.CurrencyPair.QuantityIncrement;
		}

		public static IList<Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order>> GroupOrders(this IList<Domain.Core.Entities.Trading.Order> orderEntities)
		{
			var result = new List<Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order>>();
			foreach (var openPositionOrderEntity in orderEntities.Where(entity => entity.Role == OrderRoleType.OpenPosition)
				.ToList())
			{
				result.Add(
					new Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order,
						Domain.Core.Entities.Trading.Order>(
						openPositionOrderEntity,
						orderEntities.Single(entity =>
							entity.ParentClientId == openPositionOrderEntity.ClientId && entity.Role == OrderRoleType.ClosePosition),
						orderEntities.Single(entity =>
							entity.ParentClientId == openPositionOrderEntity.ClientId && entity.Role == OrderRoleType.StopLoss)));
			}
			return result;
		}

		public static void SyncWithAnotherOrder(this Order targetOrder, Order sourceOrder)
		{
			targetOrder.ExtId = sourceOrder.ExtId;

			targetOrder.OrderSide = sourceOrder.OrderSide;
			targetOrder.OrderType = sourceOrder.OrderType;
			targetOrder.OrderStateType = sourceOrder.OrderStateType;
			targetOrder.TimeInForce = sourceOrder.TimeInForce;
		}

		public static Order Clone(this Order source, bool cloneId = false)
		{
			var newOrder = new Order
			{
				Id = cloneId ? source.Id : 0,
				ExtId = source.ExtId,
				ClientId = source.ClientId,
				ParentClientId = source.ParentClientId,
				CurrencyPair = source.CurrencyPair,
				Role = source.Role,
				OrderSide = source.OrderSide,
				OrderType = source.OrderType,
				OrderStateType = source.OrderStateType,
				TimeInForce = source.TimeInForce,
				Quantity = source.Quantity,
				Price = source.Price,
				StopPrice = source.StopPrice,
				AnalysisInfo = source.AnalysisInfo,
				Created = source.Created,
				Updated = source.Updated
			};
			return newOrder;
		}
	}
}
