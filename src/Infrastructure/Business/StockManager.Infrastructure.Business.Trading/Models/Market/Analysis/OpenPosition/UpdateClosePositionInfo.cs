using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition
{
	public class UpdateClosePositionInfo : OpenPositionInfo
	{
		public decimal ClosePrice { get; set; }
		public decimal CloseStopPrice { get; set; }

		public decimal StopLossPrice { get; set; }

		public UpdateClosePositionInfo() : base(OpenMarketPositionType.FixStopLoss) { }
	}
}
