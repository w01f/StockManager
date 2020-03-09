using System;
using StockManager.Infrastructure.Business.Trading.Enums;

namespace StockManager.Infrastructure.Business.Trading.EventArgs
{
	public class PositionChangedEventArgs : System.EventArgs
	{
		public TradingEventType EventType { get; }
		public string Details { get; }

		public PositionChangedEventArgs(TradingEventType eventType, string currencyPairId = null)
		{
			if (!String.IsNullOrWhiteSpace(currencyPairId))
				Details = $"(Ticker: {currencyPairId})";
			EventType = eventType;
		}
	}
}
