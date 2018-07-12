using System;
using System.Collections.Generic;
using System.Linq;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Models.MarketState.TrendIdentification;
using StockManager.Infrastructure.Business.Trading.Models.MarketState.TrendIdentification.Condition;
using StockManager.Infrastructure.Business.Trading.Models.MarketState.TrendIdentification.Strategy;
using StockManager.Infrastructure.Business.Trading.Models.Trading;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Business.Trading.Helpers
{
	static class TrendIdentificationHelper
	{
		private static TrandIndentificationActiveState _activeSate;

		public static MarketTrendType IdentifyMarketTrend(IList<Candle> candles, IIndicatorComputingService indicatorComputingService, TradingSettings settings)
		{
			_activeSate = new TrandIndentificationActiveState(
				candles,
				indicatorComputingService,
				settings);

			var bullishTrendIdentificationStrategy = new BullishTrendIdentificationStrategy();
			var conditionCheckingResult = bullishTrendIdentificationStrategy.CheckConditions();
			if (conditionCheckingResult.ResultType == ConditionCheckingResultType.Passed)
				return MarketTrendType.Bullish;

			var bearishTrendIdentificationStrategy = new BearishTrendIdentificationStrategy();
			conditionCheckingResult = bearishTrendIdentificationStrategy.CheckConditions();
			if (conditionCheckingResult.ResultType == ConditionCheckingResultType.Passed)
				return MarketTrendType.Bearish;

			return MarketTrendType.Accumulation;
		}

		public static ConditionCheckingResult Merge(this IList<ConditionCheckingResult> target)
		{
			return new ConditionCheckingResult
			{
				ResultType = target.Any() && target.All(conditionResult => conditionResult.ResultType == ConditionCheckingResultType.Passed) ?
					ConditionCheckingResultType.Passed :
					ConditionCheckingResultType.Failed
			};
		}

		public static IList<TIndicatorValue> GetIndicatorValues<TIndicatorValue>(IndicatorType indicatorType) where TIndicatorValue : BaseIndicatorValue
		{
			if (_activeSate.IndicatorValueCache.ContainsKey(indicatorType))
				return _activeSate.IndicatorValueCache[indicatorType].OfType<TIndicatorValue>().ToList();
			{
				var indicatorValues = new List<BaseIndicatorValue>();
				switch (indicatorType)
				{
					case IndicatorType.MACD:
						var macdSettings = _activeSate.Settings.IndicatorSettings
							.Where(indicatorSettings => indicatorSettings.Type == IndicatorType.MACD)
							.OfType<MACDSettings>()
							.Single();
						indicatorValues.AddRange(_activeSate.IndicatorComputingService.ComputeMACD(
								_activeSate.Candles,
								macdSettings.EMAPeriod1,
								macdSettings.EMAPeriod2,
								macdSettings.SignalPeriod));
						break;
					default:
						throw new ArgumentOutOfRangeException("Undefined indicator requested");
				}

				_activeSate.IndicatorValueCache.Add(indicatorType, indicatorValues);
				return indicatorValues.OfType<TIndicatorValue>().ToList();
			}
		}
	}
}
