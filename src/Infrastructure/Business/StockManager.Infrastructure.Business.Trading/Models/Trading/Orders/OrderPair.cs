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

		public bool IsPendingPosition => OpenPositionOrder.OrderStateType == OrderStateType.New ||
											OpenPositionOrder.OrderStateType == OrderStateType.Suspended;

		public bool IsOpenPosition => OpenPositionOrder.OrderStateType == OrderStateType.Filled;

		public void ApplyOrderChanges(UpdateOrderInfo marketInfo)
		{
			var now = DateTime.UtcNow;

			OpenPositionOrder.Price = marketInfo.OpenPrice;
			if (OpenPositionOrder.OrderStateType == OrderStateType.Suspended)
				OpenPositionOrder.StopPrice = marketInfo.OpenStopPrice;
			else
				OpenPositionOrder.StopPrice = null;
			OpenPositionOrder.Updated = now;

			ClosePositionOrder.Price = marketInfo.ClosePrice;
			if (ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
				ClosePositionOrder.StopPrice = marketInfo.CloseStopPrice;
			else
				ClosePositionOrder.StopPrice = null;
			ClosePositionOrder.Updated = now;

			StopLossOrder.Price = 0;
			StopLossOrder.StopPrice = marketInfo.StopLossPrice;
			StopLossOrder.Updated = now;
		}

		public void ApplyOrderChanges(UpdateClosePositionInfo marketInfo)
		{
			var now = DateTime.UtcNow;

			ClosePositionOrder.Price = marketInfo.ClosePrice;
			if (ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
				ClosePositionOrder.StopPrice = marketInfo.CloseStopPrice;
			else
				ClosePositionOrder.StopPrice = null;
			ClosePositionOrder.Updated = now;

			StopLossOrder.Price = 0;
			StopLossOrder.StopPrice = marketInfo.StopLossPrice;
			StopLossOrder.Updated = now;
		}
	}
}
