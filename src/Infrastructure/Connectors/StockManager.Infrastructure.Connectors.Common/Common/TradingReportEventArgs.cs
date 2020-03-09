using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Connectors.Common.Common
{
	public class TradingReportEventArgs
	{
		public Order ChangedOrder { get; }

		public TradingReportEventArgs(Order changedOrder)
		{
			ChangedOrder = changedOrder;
		}
	}
}
