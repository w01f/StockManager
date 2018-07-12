using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Info
{
	public class BaseMarketStateInfo
	{
		public MarketTrendType Signal { get; protected set; }
	}
}
