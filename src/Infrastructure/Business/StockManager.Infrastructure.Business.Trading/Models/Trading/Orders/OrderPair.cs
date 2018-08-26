using StockManager.Domain.Core.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Business.Trading.Models.Trading.Orders
{
	public class OrderPair
	{
		public Order InitialOrder { get; set; }
		public Order StopLossOrder { get; set; }

		public bool IsPendingPosition => (InitialOrder.OrderStateType == OrderStateType.New || InitialOrder.OrderStateType == OrderStateType.PartiallyFilled) &&
										 StopLossOrder.OrderStateType == OrderStateType.New;

		public bool IsOpenPosition => InitialOrder.OrderStateType == OrderStateType.Filled &&
										 (StopLossOrder.OrderStateType == OrderStateType.New || StopLossOrder.OrderStateType == OrderStateType.PartiallyFilled);

		public void ApplyOrderChanges(UpdateOrderInfo marketInfo)
		{
			InitialOrder.Price = marketInfo.Price;
			InitialOrder.StopPrice = marketInfo.StopPrice;

			StopLossOrder.Price = marketInfo.StopLossPrice;
			StopLossOrder.StopPrice = marketInfo.StopLossStopPrice;
		}

		public void ApplyOrderChanges(UpdateStopLossInfo marketInfo)
		{
			StopLossOrder.Price = marketInfo.Price;
			StopLossOrder.StopPrice = marketInfo.StopPrice;
		}
	}
}
