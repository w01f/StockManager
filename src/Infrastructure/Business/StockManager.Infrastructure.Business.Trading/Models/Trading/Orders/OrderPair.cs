using StockManager.Domain.Core.Common.Enums;
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
			OpenPositionOrder.Price = marketInfo.OpenPrice;
			if (OpenPositionOrder.OrderStateType == OrderStateType.Suspended)
				OpenPositionOrder.StopPrice = marketInfo.OpenStopPrice;
			else
				OpenPositionOrder.StopPrice = null;

			ClosePositionOrder.Price = marketInfo.ClosePrice;
			if (ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
				ClosePositionOrder.StopPrice = marketInfo.CloseStopPrice;
			else
				ClosePositionOrder.StopPrice = null;

			StopLossOrder.Price = marketInfo.StopLossPrice;
			StopLossOrder.StopPrice = marketInfo.StopLossPrice;
		}

		public void ApplyOrderChanges(UpdateClosePositionInfo marketInfo)
		{
			ClosePositionOrder.Price = marketInfo.ClosePrice;
			if (ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
				ClosePositionOrder.StopPrice = marketInfo.CloseStopPrice;
			else
				ClosePositionOrder.StopPrice = null;

			StopLossOrder.Price = marketInfo.StopLossPrice;
			StopLossOrder.StopPrice = marketInfo.StopLossPrice;
		}
	}
}
