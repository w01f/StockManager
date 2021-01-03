using System;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Common
{
	public class TradingEventsObserver
	{
		public event EventHandler<PositionChangedEventArgs> PositionChanged;

		public void RaisePositionChanged(TradingEventType eventType, TradingPosition position)
		{
			PositionChanged?.Invoke(this, new PositionChangedEventArgs(eventType, position));
		}

		public void RaisePositionChanged(PositionChangedEventArgs eventArgs)
		{
			PositionChanged?.Invoke(this, eventArgs);
		}
	}
}
