using StockManager.Infrastructure.Business.Trading.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition
{
	public class HoldPositionInfo : OpenPositionInfo
	{
		public HoldPositionInfo() : base(OpenMarketPositionType.Hold) { }
	}
}