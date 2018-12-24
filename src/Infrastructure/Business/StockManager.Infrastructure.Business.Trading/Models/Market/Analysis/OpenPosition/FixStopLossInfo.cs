using StockManager.Infrastructure.Business.Trading.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition
{
	public class FixStopLossInfo : OpenPositionInfo
	{
		public decimal StopLossPrice { get; set; }

		public FixStopLossInfo() : base(OpenMarketPositionType.FixStopLoss) { }
		public FixStopLossInfo(OpenMarketPositionType infoType) : base(infoType) { }
	}
}
