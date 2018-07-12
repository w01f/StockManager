using StockManager.Infrastructure.Business.Trading.Models.Trading;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Info
{
	abstract class ActiveMarketInfo : BaseMarketStateInfo
	{
		public TradingData TradingData { get; set; }
	}
}
