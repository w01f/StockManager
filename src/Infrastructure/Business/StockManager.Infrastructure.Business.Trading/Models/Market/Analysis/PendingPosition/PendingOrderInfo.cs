using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition
{
	public class PendingOrderInfo : PendingPositionInfo
	{
		public PendingOrderInfo() : base(PendingMarketPositionType.Hold) { }
	}
}