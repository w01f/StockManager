using System;
using StockManager.Infrastructure.Business.Trading.Enums;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Common
{
	public class TradingEventsObserver
	{
		public event EventHandler<PositionChangedEventArgs> PositionChanged;

		public void RaisePositionChanged(TradingEventType eventType)
		{
			PositionChanged?.Invoke(this, new PositionChangedEventArgs { EventType = eventType });
		}

		public class PositionChangedEventArgs : EventArgs
		{
			public TradingEventType EventType { get; set; }
		}
	}
}
