using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Business.Trading.Models.Trading.Positions
{
	public class TradingPosition
	{
		public Order OpenPositionOrder { get; set; }
		public Order ClosePositionOrder { get; set; }
		public Order StopLossOrder { get; set; }

		public CurrencyPair CurrencyPair => OpenPositionOrder.CurrencyPair;
		public string CurrencyPairId => CurrencyPair.Id;

		public bool IsPendingPosition => OpenPositionOrder.OrderStateType == OrderStateType.New ||
											OpenPositionOrder.OrderStateType == OrderStateType.Suspended;

		public bool IsOpenPosition => OpenPositionOrder.OrderStateType == OrderStateType.Filled;
		public bool IsClosedPosition => OpenPositionOrder.OrderStateType == OrderStateType.Cancelled || (ClosePositionOrder.OrderStateType == OrderStateType.Cancelled || StopLossOrder.OrderStateType == OrderStateType.Cancelled);
	}
}
