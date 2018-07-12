using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Info
{
	class AccumulationMarketInfo : BaseMarketStateInfo
	{
		public AccumulationMarketInfo()
		{
			Signal = MarketTrendType.Accumulation;
		}
	}
}
