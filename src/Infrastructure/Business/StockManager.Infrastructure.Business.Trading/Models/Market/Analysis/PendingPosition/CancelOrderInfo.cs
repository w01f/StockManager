using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition
{
	class CancelOrderInfo : PendingPositionInfo
	{
		public CancelOrderInfo() : base(PendingMarketPositionType.CancelOrder) { }
	}
}