using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis.Condition
{
	class OverboughtBearishCondition : BaseCondition
	{
		public override async Task<ConditionCheckingResult> Check(
			TradingSettings settings,
			IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService
		)
		{
			var conditionCheckingResult = new ConditionCheckingResult();

			var rsiSettings = new CommonIndicatorSettings()
			{
				Period = 14
			};

			var candleRangeSize = new[] { rsiSettings.RequiredCandleRangeSize }.Max();

			var candles = (await CandleLoader.Load(
				settings.CurrencyPairId,
				settings.Period,
				candleRangeSize,
				settings.CurrentMoment,
				candleRepository,
				marketDataConnector)).ToList();

			var rsiValues = indicatorComputingService.ComputeRelativeStrengthIndex(
					candles,
					rsiSettings.Period)
				.OfType<SimpleIndicatorValue>()
				.ToList();

			var curentRSIValue = rsiValues.ElementAtOrDefault(rsiValues.Count - 1);
			var previousRSIValue = rsiValues.ElementAtOrDefault(rsiValues.Count - 2);

			if (curentRSIValue == null || previousRSIValue == null)
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			//if RSI turning from maximum
			if (!(curentRSIValue.Value.HasValue &&
				  previousRSIValue.Value.HasValue &&
				  curentRSIValue.Value < previousRSIValue.Value))
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;

			return conditionCheckingResult;
		}
	}
}
