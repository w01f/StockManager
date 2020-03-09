using System;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Common
{
	public class TradingEventsObserver
	{
		public event EventHandler<PositionChangedEventArgs> PositionChanged;

		public void RaisePositionChanged(TradingEventType eventType, string currencyPairId)
		{
			PositionChanged?.Invoke(this, new PositionChangedEventArgs(eventType, currencyPairId));
		}

		public void RaisePositionChanged(PositionChangedEventArgs eventArgs)
		{
			PositionChanged?.Invoke(this, eventArgs);
		}
	}
}
