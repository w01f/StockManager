using System;
using StockManager.Infrastructure.Business.Trading.Enums;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Common
{
	public class TradingEventsObserver
	{
		public event EventHandler<PositionChangedEventArgs> PositionChanged;

		public void RaisePositionChanged(TradingEventType eventType, string details)
		{
			PositionChanged?.Invoke(this, new PositionChangedEventArgs(eventType, details));
		}

		public class PositionChangedEventArgs : EventArgs
		{
			public TradingEventType EventType { get; }
			public string Details { get; }

			public PositionChangedEventArgs(TradingEventType eventType, string details)
			{
				Details = details;
				EventType = eventType;
			}
		}
	}
}
