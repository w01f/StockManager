using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis.Condition;
using StockManager.Infrastructure.Business.Trading.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis.Strategy
{
	abstract class BaseAnalysisStrategy
	{
		protected readonly List<BaseCondition> _conditions = new List<BaseCondition>();

		protected abstract void LoadConditions();

		public async Task<ConditionCheckingResult> CheckConditions(
			TradingSettings settings,
			IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService
			)
		{
			LoadConditions();

			foreach (var condition in _conditions)
			{
				var conditionResult = await condition.Check(settings, candleRepository, marketDataConnector, indicatorComputingService);
				if (conditionResult.ResultType == ConditionCheckingResultType.Failed)
					return conditionResult;
			}

			return new ConditionCheckingResult { ResultType = ConditionCheckingResultType.Passed };
		}
	}
}
