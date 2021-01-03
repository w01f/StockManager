using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;

namespace StockManager.Infrastructure.Business.Trading.EventArgs
{
	public class PositionChangedEventArgs : System.EventArgs
	{
		public TradingEventType EventType { get; }
		public TradingPosition Position { get; }

		public string Details => Position != null ? $"(Ticker: {Position.CurrencyPairId})" : null;

		public PositionChangedEventArgs(TradingEventType eventType, TradingPosition position)
		{
			EventType = eventType;
			Position = position;
		}
	}
}
