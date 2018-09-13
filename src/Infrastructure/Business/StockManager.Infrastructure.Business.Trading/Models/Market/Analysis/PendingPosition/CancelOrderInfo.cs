using StockManager.Infrastructure.Business.Trading.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition
{
	class CancelOrderInfo : PendingPositionInfo
	{
		public CancelOrderInfo() : base(PendingMarketPositionType.CancelOrder) { }
	}
}