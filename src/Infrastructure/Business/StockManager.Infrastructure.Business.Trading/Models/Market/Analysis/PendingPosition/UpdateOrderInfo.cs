using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition
{
	public class UpdateOrderInfo : PendingPositionInfo
	{
		public decimal OpenPrice { get; set; }
		public decimal OpenStopPrice { get; set; }

		public decimal ClosePrice { get; set; }
		public decimal CloseStopPrice { get; set; }

		public decimal StopLossPrice { get; set; }

		public UpdateOrderInfo() : base(PendingMarketPositionType.UpdateOrder) { }
	}
}
