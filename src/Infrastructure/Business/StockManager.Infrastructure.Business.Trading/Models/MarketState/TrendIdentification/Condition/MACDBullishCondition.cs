using System.Linq;
using StockManager.Infrastructure.Analysis.Common.Helpers;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Helpers;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.TrendIdentification.Condition
{
	class MACDBullishCondition : BaseTrendingIdentificationCondition
	{
		public override ConditionCheckingResult Check()
		{
			var conditionCheckingResult = new ConditionCheckingResult();

			var indicatorValues = TrendIdentificationHelper.GetIndicatorValues<MACDValue>(IndicatorType.MACD);

			var itemsCount = indicatorValues.Count;
			var curentMomentValue = indicatorValues.ElementAtOrDefault(indicatorValues.Count - 1);
			var previousMomentValue = indicatorValues.ElementAtOrDefault(indicatorValues.Count - 2);

			//if MACD lower then Signal
			if (!(curentMomentValue.MACD.HasValue &&
				  curentMomentValue.Signal.HasValue &&
				  curentMomentValue.MACD.Value < curentMomentValue.Signal.Value))
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			//if MACD turning from minimum
			var avgMACDMinimum = indicatorValues
				.Where(value => value.MACD.HasValue)
				.Select(value => value.MACD.Value)
				.ToList()
				.GetAverageMinimum();
			if (!(curentMomentValue.MACD.HasValue &&
				  previousMomentValue.MACD.HasValue &&
				  curentMomentValue.MACD > previousMomentValue.MACD &&
				  indicatorValues.Skip(itemsCount - 5).Min(value => value.MACD) < avgMACDMinimum))
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			//If Histogram turning from minimum
			var avgHistogramMinimum = indicatorValues
				.Where(value => value.Histogram.HasValue)
				.Select(value => value.Histogram.Value)
				.ToList()
				.GetAverageMinimum();
			if (!(curentMomentValue.Histogram.HasValue &&
				previousMomentValue.Histogram.HasValue &&
				  curentMomentValue.Histogram > previousMomentValue.Histogram &&
				  indicatorValues.Skip(itemsCount - 5).Min(value => value.Histogram) < avgHistogramMinimum))
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;

			return conditionCheckingResult;
		}
	}
}
