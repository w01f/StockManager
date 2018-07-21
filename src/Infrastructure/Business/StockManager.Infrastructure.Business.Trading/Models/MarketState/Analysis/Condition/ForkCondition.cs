using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Domain.Core.Entities.Market;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis.Condition
{
	class ForkCondition : BaseCondition
	{
		private readonly BaseCondition _fork;
		private readonly IList<BaseCondition> _passed;
		private readonly IList<BaseCondition> _failed;

		public ForkCondition(BaseCondition fork, IList<BaseCondition> passed, IList<BaseCondition> failed)
		{
			_fork = fork;
			_passed = passed;
			_failed = failed;
		}

		public override async Task<ConditionCheckingResult> Check(
			TradingSettings settings,
			IRepository<Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService)
		{
			var conditionCheckingResult = new ConditionCheckingResult { ResultType = ConditionCheckingResultType.Failed };

			if (_fork == null) return conditionCheckingResult;

			var forkResult = await _fork.Check(
				settings,
				candleRepository,
				marketDataConnector,
				indicatorComputingService);

			var conditions = forkResult.ResultType == ConditionCheckingResultType.Passed ?
				_passed :
				_failed;

			if (conditions == null) return conditionCheckingResult;

			foreach (var condition in conditions)
			{
				var conditionResult = await condition.Check(settings, candleRepository, marketDataConnector, indicatorComputingService);
				if (conditionResult.ResultType == ConditionCheckingResultType.Failed)
					return conditionResult;
			}

			conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;

			return conditionCheckingResult;
		}
	}
}
