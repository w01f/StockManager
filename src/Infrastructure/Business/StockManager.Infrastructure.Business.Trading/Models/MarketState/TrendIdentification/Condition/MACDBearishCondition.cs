using System.Linq;
using StockManager.Infrastructure.Analysis.Common.Helpers;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Helpers;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.TrendIdentification.Condition
{
	class MACDBearishCondition : BaseTrendingIdentificationCondition
	{
		public override ConditionCheckingResult Check()
		{
			var conditionCheckingResult = new ConditionCheckingResult();

			var indicatorValues = TrendIdentificationHelper.GetIndicatorValues<MACDValue>(IndicatorType.MACD);

			var itemsCount = indicatorValues.Count;
			var curentMomentValue = indicatorValues.ElementAtOrDefault(indicatorValues.Count - 1);
			var previousMomentValue = indicatorValues.ElementAtOrDefault(indicatorValues.Count - 2);

			//if MACD higher then Signal
			if (!(curentMomentValue.MACD.HasValue &&
				  curentMomentValue.Signal.HasValue &&
				  curentMomentValue.MACD.Value > curentMomentValue.Signal.Value))
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			//if MACD turning from maximum
			var avgMACDMaximum = indicatorValues
				.Where(value => value.MACD.HasValue)
				.Select(value => value.MACD.Value)
				.ToList()
				.GetAverageMaximum();
			if (!(curentMomentValue.MACD.HasValue &&
				  previousMomentValue.MACD.HasValue &&
				  curentMomentValue.MACD < previousMomentValue.MACD &&
				  indicatorValues.Skip(itemsCount - 5).Max(value => value.MACD) > avgMACDMaximum))
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			//If Histogram turning from maximum
			var avgHistogramMaximum = indicatorValues
				.Where(value => value.Histogram.HasValue)
				.Select(value => value.Histogram.Value)
				.ToList()
				.GetAverageMaximum();
			if (!(curentMomentValue.Histogram.HasValue &&
				previousMomentValue.Histogram.HasValue &&
				  curentMomentValue.Histogram < previousMomentValue.Histogram &&
				  indicatorValues.Skip(itemsCount - 5).Max(value => value.Histogram) > avgHistogramMaximum))
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;

			return conditionCheckingResult;
		}
	}
}
