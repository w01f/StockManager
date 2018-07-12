using System.Collections.Generic;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.MarketState.TrendIdentification.Condition;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.TrendIdentification.Strategy
{
	abstract class BaseTrendIdentificationStrategy
	{
		protected readonly List<BaseTrendingIdentificationCondition> _conditions = new List<BaseTrendingIdentificationCondition>();

		protected abstract void LoadConditions();

		public ConditionCheckingResult CheckConditions()
		{
			LoadConditions();

			var conditionResults = new List<ConditionCheckingResult>();
			foreach (var condition in _conditions)
				conditionResults.Add(condition.Check());

			return conditionResults.Merge();
		}
	}
}
