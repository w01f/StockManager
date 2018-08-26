using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition
{
	public class UpdateStopLossInfo : OpenPositionInfo
	{
		public decimal Price { get; set; }
		public decimal StopPrice { get; set; }

		public UpdateStopLossInfo() : base(OpenMarketPositionType.FixStopLoss) { }
	}
}
