using StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis.Condition;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis.Strategy
{
	class SellMarketStrategy : BaseAnalysisStrategy
	{
		protected override void LoadConditions()
		{
			_conditions.Add(new ForkCondition(
				new BullishTrendCondition(),
				new BaseCondition[] { new OverboughtBullishCondition() },
				new BaseCondition[] { new OverboughtBearishCondition() }
				));
		}
	}
}
