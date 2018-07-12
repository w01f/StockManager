namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.TrendIdentification.Condition
{
	abstract class BaseTrendingIdentificationCondition
	{
		public abstract ConditionCheckingResult Check();
	}
}
