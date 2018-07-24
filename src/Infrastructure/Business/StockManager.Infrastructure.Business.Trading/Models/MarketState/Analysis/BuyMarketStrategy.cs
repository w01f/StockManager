using System;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Helpers;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis
{
	class BuyMarketStrategy //: BaseAnalysisStrategy
	{
		public async Task<ConditionCheckingResult> CheckConditions(
			TradingSettings settings,
			IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService
		)
		{
			var conditionCheckingResult = new ConditionCheckingResult();

			var firstFrameMACDSettings = new MACDSettings
			{
				EMAPeriod1 = 12,
				EMAPeriod2 = 26,
				SignalPeriod = 9
			};

			var firtsFrameCandles = (await CandleLoader.Load(
				settings.CurrencyPairId,
				settings.Period.GetHigherFramePeriod(),
				firstFrameMACDSettings.RequiredCandleRangeSize,
				settings.CurrentMoment,
				candleRepository,
				marketDataConnector)).ToList();

			var firstFrameMACDValues = indicatorComputingService.ComputeMACD(
					firtsFrameCandles,
					firstFrameMACDSettings.EMAPeriod1,
					firstFrameMACDSettings.EMAPeriod2,
					firstFrameMACDSettings.SignalPeriod)
				.OfType<MACDValue>()
				.ToList();

			var firstFrameCurrentMACDValue = firstFrameMACDValues.ElementAtOrDefault(firstFrameMACDValues.Count - 1);
			var firstFrameOnePreviouseMACDValue = firstFrameMACDValues.ElementAtOrDefault(firstFrameMACDValues.Count - 2);

			//If all valuable parameters are not null
			if (firstFrameCurrentMACDValue?.MACD == null ||
				firstFrameCurrentMACDValue.Signal == null ||
				firstFrameOnePreviouseMACDValue?.MACD == null ||
				firstFrameOnePreviouseMACDValue.Signal == null)
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			//if MACD higher then Signal then it is Bullish trend
			if (!(firstFrameCurrentMACDValue.MACD.Value > firstFrameCurrentMACDValue.Signal.Value))
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			var isGrowingBullishTrend = firstFrameCurrentMACDValue.Histogram > firstFrameOnePreviouseMACDValue.Histogram &&
				(Math.Abs(firstFrameCurrentMACDValue.MACD.Value - firstFrameCurrentMACDValue.Signal.Value) >
					Math.Abs(firstFrameOnePreviouseMACDValue.MACD.Value - firstFrameOnePreviouseMACDValue.Signal.Value) ||
				firstFrameOnePreviouseMACDValue.MACD.Value < firstFrameOnePreviouseMACDValue.Signal.Value);

			var secondFrameRSISettings = new CommonIndicatorSettings()
			{
				Period = 14
			};

			var secondFrameMACDSettings = new MACDSettings
			{
				EMAPeriod1 = 12,
				EMAPeriod2 = 26,
				SignalPeriod = 9
			};

			var candleRangeSize = new[] { secondFrameMACDSettings.RequiredCandleRangeSize, secondFrameRSISettings.RequiredCandleRangeSize }.Max();

			var secondFrameCandles = (await CandleLoader.Load(
				settings.CurrencyPairId,
				settings.Period,
				candleRangeSize,
				settings.CurrentMoment,
				candleRepository,
				marketDataConnector)).ToList();

			var secondFrameRSIValues = indicatorComputingService.ComputeRelativeStrengthIndex(
					secondFrameCandles,
					secondFrameRSISettings.Period)
				.OfType<SimpleIndicatorValue>()
				.ToList();

			var secondFrameCurentRSIValue = secondFrameRSIValues.ElementAtOrDefault(secondFrameRSIValues.Count - 1);
			var secondFrameOnePreviouseRSIValue = secondFrameRSIValues.ElementAtOrDefault(secondFrameRSIValues.Count - 2);
			var secondFrameTwoPreviouseRSIValue = secondFrameRSIValues.ElementAtOrDefault(secondFrameRSIValues.Count - 3);

			if (secondFrameCurentRSIValue?.Value == null ||
				secondFrameOnePreviouseRSIValue?.Value == null ||
				secondFrameTwoPreviouseRSIValue?.Value == null)
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			var secondFrameMaxRSIValue = secondFrameRSIValues
				.Where(value => value.Value.HasValue)
				.Select(value => value.Value.Value)
				.ToList()
				.GetMaximumValues()
				.Max();

			var lowRangeBroder = secondFrameMaxRSIValue * 2 / 3;

			//if RSI turning from minimum 
			//if Prev RSI lower then lowRangeBroder
			if (!(secondFrameCurentRSIValue.Value > secondFrameOnePreviouseRSIValue.Value &&
				  secondFrameTwoPreviouseRSIValue.Value > secondFrameOnePreviouseRSIValue.Value &&
				  secondFrameOnePreviouseRSIValue.Value < lowRangeBroder))
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			if (!isGrowingBullishTrend)
			{
				var secondFrameMACDValues = indicatorComputingService.ComputeMACD(
						secondFrameCandles,
						secondFrameMACDSettings.EMAPeriod1,
						secondFrameMACDSettings.EMAPeriod2,
						secondFrameMACDSettings.SignalPeriod)
					.OfType<MACDValue>()
					.ToList();

				var secondFrameCurentMACDValue = secondFrameMACDValues.ElementAtOrDefault(firstFrameMACDValues.Count - 1);
				var secondFrameOnePreviouseMACDValue = secondFrameMACDValues.ElementAtOrDefault(firstFrameMACDValues.Count - 2);

				//If all valuable parameters are not null
				if (secondFrameCurentMACDValue?.MACD == null ||
					secondFrameCurentMACDValue.Signal == null ||
					secondFrameCurentMACDValue.Histogram == null ||
					secondFrameOnePreviouseMACDValue?.MACD == null ||
					secondFrameOnePreviouseMACDValue.Signal == null ||
					secondFrameOnePreviouseMACDValue.Histogram == null)
				{
					conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
					return conditionCheckingResult;
				}

				//If Histogram is lower then avg minimum
				var avgHistogramMinimum = secondFrameMACDValues
					.Where(value => value.Histogram.HasValue)
					.Select(value => value.Histogram.Value)
					.ToList()
					.GetAverageMinimum();
				if (secondFrameCurentMACDValue.Histogram > secondFrameOnePreviouseMACDValue.Histogram || 
					secondFrameMACDValues.Skip(candleRangeSize - IndicatorSettings.DeviationSize).Min(value => value.Histogram) < avgHistogramMinimum) 
				{
					conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;
					return conditionCheckingResult;
				}

				if (!(secondFrameCurentMACDValue.MACD.Value > 0 &&
					  secondFrameCurentMACDValue.MACD.Value > secondFrameCurentMACDValue.Signal.Value &&
					  (Math.Abs(secondFrameCurentMACDValue.MACD.Value - secondFrameCurentMACDValue.Signal.Value) >
					   Math.Abs(secondFrameOnePreviouseMACDValue.MACD.Value - secondFrameOnePreviouseMACDValue.Signal.Value) ||
					   secondFrameOnePreviouseMACDValue.MACD.Value < secondFrameOnePreviouseMACDValue.Signal.Value)))
				{
					conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
					return conditionCheckingResult;
				}
			}

			conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;
			return conditionCheckingResult;
		}
	}
}
