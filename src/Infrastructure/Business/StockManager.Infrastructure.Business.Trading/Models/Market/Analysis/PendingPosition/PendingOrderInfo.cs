using StockManager.Infrastructure.Business.Trading.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition
{
	public class PendingOrderInfo : PendingPositionInfo
	{
		public PendingOrderInfo() : base(PendingMarketPositionType.Hold) { }
	}
}