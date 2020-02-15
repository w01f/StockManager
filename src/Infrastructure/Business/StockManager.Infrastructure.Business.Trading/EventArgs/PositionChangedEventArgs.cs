using StockManager.Infrastructure.Business.Trading.Enums;

namespace StockManager.Infrastructure.Business.Trading.EventArgs
{
	public class PositionChangedEventArgs : System.EventArgs
	{
		public TradingEventType EventType { get; }
		public string Details { get; }

		public PositionChangedEventArgs(TradingEventType eventType, string details = null)
		{
			Details = details;
			EventType = eventType;
		}
	}
}
