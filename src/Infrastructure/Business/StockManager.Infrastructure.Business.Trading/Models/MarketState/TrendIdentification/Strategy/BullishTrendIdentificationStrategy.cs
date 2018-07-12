using StockManager.Infrastructure.Business.Trading.Models.MarketState.TrendIdentification.Condition;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.TrendIdentification.Strategy
{
	class BullishTrendIdentificationStrategy : BaseTrendIdentificationStrategy
	{
		protected override void LoadConditions()
		{
			_conditions.Add(new MACDBullishCondition());
		}
	}
}
