using System;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Business.Trading.Models.Trading.Orders
{
	public class OrderPair
	{
		public Order OpenPositionOrder { get; set; }
		public Order ClosePositionOrder { get; set; }
		public Order StopLossOrder { get; set; }

		public string CurrencyPairId => OpenPositionOrder.CurrencyPair.Id;

		public bool IsPendingPosition => OpenPositionOrder.OrderStateType == OrderStateType.New ||
											OpenPositionOrder.OrderStateType == OrderStateType.Suspended;

		public bool IsOpenPosition => OpenPositionOrder.OrderStateType == OrderStateType.Filled;

		public void ApplyOrderChanges(UpdateOrderInfo marketInfo)
		{
			var now = DateTime.UtcNow;

			OpenPositionOrder.Price = marketInfo.OpenPrice;
			if (OpenPositionOrder.OrderStateType == OrderStateType.Suspended)
			{
				OpenPositionOrder.StopPrice = marketInfo.OpenStopPrice;
				OpenPositionOrder.OrderType = OrderType.StopLimit;
			}
			else
			{
				OpenPositionOrder.StopPrice = null;
				OpenPositionOrder.OrderType = OrderType.Limit;
			}

			OpenPositionOrder.Updated = now;

			ClosePositionOrder.Price = marketInfo.ClosePrice;
			if (ClosePositionOrder.OrderStateType == OrderStateType.Pending ||
				ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
			{
				ClosePositionOrder.StopPrice = marketInfo.CloseStopPrice;
				ClosePositionOrder.OrderType = OrderType.StopLimit;
			}
			else
			{
				ClosePositionOrder.StopPrice = null;
				ClosePositionOrder.OrderType = OrderType.Limit;
			}
			ClosePositionOrder.Updated = now;

			StopLossOrder.Price = 0;
			StopLossOrder.StopPrice = marketInfo.StopLossPrice;
			StopLossOrder.Updated = now;
		}

		public void ApplyOrderChanges(UpdateClosePositionInfo marketInfo)
		{
			var now = DateTime.UtcNow;

			ClosePositionOrder.Price = marketInfo.ClosePrice;
			if (ClosePositionOrder.OrderStateType == OrderStateType.Pending ||
				ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
			{
				ClosePositionOrder.OrderStateType = OrderStateType.Suspended;
				ClosePositionOrder.StopPrice = marketInfo.CloseStopPrice;
				ClosePositionOrder.OrderType = OrderType.StopLimit;
			}
			else
			{
				ClosePositionOrder.StopPrice = null;
				ClosePositionOrder.OrderType = OrderType.Limit;
			}
			ClosePositionOrder.Updated = now;

			StopLossOrder.Price = 0;
			StopLossOrder.StopPrice = marketInfo.StopLossPrice;
			StopLossOrder.Updated = now;
		}

		public void ApplyOrderChanges(FixStopLossInfo marketInfo)
		{
			var now = DateTime.UtcNow;

			StopLossOrder.Price = 0;
			StopLossOrder.StopPrice = marketInfo.StopLossPrice;
			StopLossOrder.Updated = now;
		}
	}
}
