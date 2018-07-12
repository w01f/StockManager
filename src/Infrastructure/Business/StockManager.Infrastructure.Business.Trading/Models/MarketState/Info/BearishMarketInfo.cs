using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Info
{
	class BearishMarketInfo : ActiveMarketInfo
	{
		public BearishMarketInfo()
		{
			Signal = MarketTrendType.Bearish;
		}
	}
}
