using System;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Utilities.Configuration.Models;

namespace StockManager.Infrastructure.Business.Trading.Helpers
{
	static class TradingPositionHelper
	{
		public static TradingPosition CreatePosition(NewOrderPositionInfo positionInfo, CurrencyPair currencyPair, TradingSettings tradingSettings)
		{
			var now = DateTime.UtcNow;
			var parentClientId = Guid.NewGuid();

			var position = new TradingPosition
			{
				OpenPositionOrder = new Order
				{
					ClientId = parentClientId,
					CurrencyPair = currencyPair,
					Role = OrderRoleType.OpenPosition,
					OrderSide = tradingSettings.BaseOrderSide,
					OrderType = OrderType.StopLimit,
					OrderStateType = OrderStateType.Suspended,
					TimeInForce = OrderTimeInForceType.GoodTillCancelled,
					Price = positionInfo.OpenPrice,
					StopPrice = positionInfo.OpenStopPrice,
					Created = now,
					Updated = now
				},

				ClosePositionOrder = new Order
				{
					ClientId = Guid.NewGuid(),
					ParentClientId = parentClientId,
					CurrencyPair = currencyPair,
					Role = OrderRoleType.ClosePosition,
					OrderSide = tradingSettings.OppositeOrderSide,
					OrderType = OrderType.StopLimit,
					OrderStateType = OrderStateType.Pending,
					TimeInForce = OrderTimeInForceType.GoodTillCancelled,
					Price = positionInfo.ClosePrice,
					StopPrice = positionInfo.CloseStopPrice,
					Created = now,
					Updated = now
				},

				StopLossOrder = new Order
				{
					ClientId = Guid.NewGuid(),
					ParentClientId = parentClientId,
					CurrencyPair = currencyPair,
					Role = OrderRoleType.StopLoss,
					OrderSide = tradingSettings.OppositeOrderSide,
					OrderType = OrderType.StopMarket,
					OrderStateType = OrderStateType.Pending,
					TimeInForce = OrderTimeInForceType.GoodTillCancelled,
					Price = 0,
					StopPrice = positionInfo.StopLossPrice,
					Created = now,
					Updated = now
				}
			};

			return position;
		}

		public static void ChangePosition(this TradingPosition tradingPosition, UpdateOrderInfo marketInfo)
		{
			var now = DateTime.UtcNow;

			tradingPosition.OpenPositionOrder.Price = marketInfo.OpenPrice;
			if (tradingPosition.OpenPositionOrder.OrderStateType == OrderStateType.Suspended)
			{
				//tradingPosition.OpenPositionOrder.StopPrice = marketInfo.OpenStopPrice;
				tradingPosition.OpenPositionOrder.OrderType = OrderType.StopLimit;
			}
			else
			{
				tradingPosition.OpenPositionOrder.OrderStateType = OrderStateType.New;
				tradingPosition.OpenPositionOrder.StopPrice = null;
				tradingPosition.OpenPositionOrder.OrderType = OrderType.Limit;
			}
			tradingPosition.OpenPositionOrder.Updated = now;

			tradingPosition.ClosePositionOrder.Price = marketInfo.ClosePrice;
			tradingPosition.ClosePositionOrder.StopPrice = marketInfo.CloseStopPrice;
			tradingPosition.ClosePositionOrder.OrderType = OrderType.StopLimit;
			tradingPosition.ClosePositionOrder.OrderStateType = OrderStateType.Pending;
			tradingPosition.ClosePositionOrder.Updated = now;

			tradingPosition.StopLossOrder.Price = 0;
			tradingPosition.StopLossOrder.StopPrice = marketInfo.StopLossPrice;
			tradingPosition.StopLossOrder.OrderStateType = OrderStateType.Pending;
			tradingPosition.StopLossOrder.Updated = now;
		}

		public static void ChangePosition(this TradingPosition tradingPosition, UpdateClosePositionInfo marketInfo)
		{
			var now = DateTime.UtcNow;

			tradingPosition.ClosePositionOrder.Price = marketInfo.ClosePrice;
			if (tradingPosition.ClosePositionOrder.OrderStateType == OrderStateType.Pending ||
				tradingPosition.ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
			{
				tradingPosition.ClosePositionOrder.OrderStateType = OrderStateType.Suspended;
				tradingPosition.ClosePositionOrder.StopPrice = marketInfo.CloseStopPrice;
				tradingPosition.ClosePositionOrder.OrderType = OrderType.StopLimit;
			}
			else
			{
				tradingPosition.ClosePositionOrder.OrderStateType = OrderStateType.New;
				tradingPosition.ClosePositionOrder.StopPrice = null;
				tradingPosition.ClosePositionOrder.OrderType = OrderType.Limit;
			}
			tradingPosition.ClosePositionOrder.Updated = now;

			tradingPosition.StopLossOrder.Price = 0;
			tradingPosition.StopLossOrder.StopPrice = marketInfo.StopLossPrice;
			tradingPosition.StopLossOrder.Updated = now;
		}

		public static void ChangePosition(this TradingPosition tradingPosition, FixStopLossInfo marketInfo)
		{
			var now = DateTime.UtcNow;

			tradingPosition.StopLossOrder.Price = 0;
			tradingPosition.StopLossOrder.StopPrice = marketInfo.StopLossPrice;
			tradingPosition.StopLossOrder.Updated = now;
		}

		public static void ChangePosition(this TradingPosition tradingPosition, SuspendPositionInfo marketInfo)
		{
			tradingPosition.ClosePositionOrder.OrderStateType = OrderStateType.Pending;
		}

		public static void ChangePosition(this TradingPosition tradingPosition, CancelOrderInfo marketInfo)
		{
			tradingPosition.OpenPositionOrder.OrderStateType = OrderStateType.Cancelled;
		}

		public static void SyncWithAnotherPosition(this TradingPosition targetPosition, TradingPosition sourcePosition, bool syncPrice = false)
		{
			targetPosition.OpenPositionOrder.SyncWithAnotherOrder(sourcePosition.OpenPositionOrder, syncPrice);
			targetPosition.ClosePositionOrder.SyncWithAnotherOrder(sourcePosition.ClosePositionOrder, syncPrice);
			targetPosition.StopLossOrder.SyncWithAnotherOrder(sourcePosition.StopLossOrder, syncPrice);
		}

		public static TradingPosition Clone(this TradingPosition source)
		{
			return new TradingPosition
			{
				OpenPositionOrder = source.OpenPositionOrder.Clone(true),
				ClosePositionOrder = source.ClosePositionOrder.Clone(true),
				StopLossOrder = source.StopLossOrder.Clone(true)
			};
		}
	}
}
