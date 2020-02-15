using System;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Common
{
	public class TradingEventsObserver
	{
		public event EventHandler<PositionChangedEventArgs> PositionChanged;

		public void RaisePositionChanged(TradingEventType eventType, string details)
		{
			PositionChanged?.Invoke(this, new PositionChangedEventArgs(eventType, details));
		}
	}
}
