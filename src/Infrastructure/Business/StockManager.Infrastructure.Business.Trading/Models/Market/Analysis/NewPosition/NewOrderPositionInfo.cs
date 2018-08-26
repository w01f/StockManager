using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition
{
	public class NewOrderPositionInfo : NewPositionInfo
	{
		public decimal Price { get; set; }
		public decimal StopPrice { get; set; }

		public decimal StopLossPrice { get; set; }
		public decimal StopLossStopPrice { get; set; }


		public NewOrderPositionInfo(NewMarketPositionType positionType) : base(positionType)
		{
		}
	}
}
