using StockManager.Infrastructure.Business.Trading.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition
{
	public class NewOrderPositionInfo : NewPositionInfo
	{
		public string CurrencyPairId { get; set; }

		public decimal OpenPrice { get; set; }
		public decimal OpenStopPrice { get; set; }

		public decimal ClosePrice { get; set; }
		public decimal CloseStopPrice { get; set; }

		public decimal StopLossPrice { get; set; }

		public NewOrderPositionInfo(NewMarketPositionType positionType) : base(positionType)
		{
		}
	}
}
