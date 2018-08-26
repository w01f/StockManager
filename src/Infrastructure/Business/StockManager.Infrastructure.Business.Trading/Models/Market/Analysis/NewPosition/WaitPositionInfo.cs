using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition
{
	public class WaitPositionInfo : NewPositionInfo
	{
		public WaitPositionInfo() : base(NewMarketPositionType.Wait){}
	}
}
