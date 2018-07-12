using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Info
{
	class BullishMarketInfo : ActiveMarketInfo
	{
		public BullishMarketInfo()
		{
			Signal = MarketTrendType.Bullish;
		}
	}
}
