using StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis.Condition;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis.Strategy
{
	class BuyMarketStrategy : BaseAnalysisStrategy
	{
		protected override void LoadConditions()
		{
			_conditions.Add(new BullishTrendCondition());
			_conditions.Add(new OversoldnessCondition());
		}
	}
}
