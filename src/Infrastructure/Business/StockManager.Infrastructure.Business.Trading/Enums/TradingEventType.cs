namespace StockManager.Infrastructure.Business.Trading.Enums
{
	public enum TradingEventType
	{
		NewPosition,
		PositionOpened,
		PositionCancelled,
		PositionClosedSuccessfully,
		PositionClosedDueCancel,
		PositionClosedDueStopLoss
	}
}
