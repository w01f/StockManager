using StockManager.Infrastructure.Business.Trading.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition
{
	public class UpdateClosePositionInfo : FixStopLossInfo
	{
		public decimal ClosePrice { get; set; }
		public decimal CloseStopPrice { get; set; }

		public UpdateClosePositionInfo() : base(OpenMarketPositionType.UpdateOrder) { }
	}
}
