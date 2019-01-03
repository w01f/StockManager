using StockManager.Infrastructure.Business.Trading.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition
{
	class SuspendPositionInfo : OpenPositionInfo
	{
		public SuspendPositionInfo() : base(OpenMarketPositionType.Suspend) { }
	}
}
