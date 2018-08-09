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
			var conditionCheckingResult = new ConditionCheckingResult() { ResultType = ConditionCheckingResultType.Failed };

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
				firstFrameCurrentMACDValue.Histogram == null ||
				firstFrameOnePreviouseMACDValue?.MACD == null ||
				firstFrameOnePreviouseMACDValue.Signal == null ||
				firstFrameOnePreviouseMACDValue.Histogram == null)
			{
				return conditionCheckingResult;
			}

			//if MACD higher then Signal then it is Bullish trend
			//if Histogram is rising 
			if (!(Math.Round(firstFrameCurrentMACDValue.MACD.Value - firstFrameCurrentMACDValue.Signal.Value, 4) >= 0))
			{
				return conditionCheckingResult;
			}

			var isBullishTrendRising = Math.Round(firstFrameCurrentMACDValue.Histogram.Value - firstFrameOnePreviouseMACDValue.Histogram.Value, 5) >= 0 &&
				(firstFrameCurrentMACDValue.MACD.Value - firstFrameCurrentMACDValue.Signal.Value > firstFrameOnePreviouseMACDValue.MACD.Value - firstFrameOnePreviouseMACDValue.Signal.Value ||
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

			var secondFrameCurrentCandle = secondFrameCandles.ElementAtOrDefault(secondFrameCandles.Count - 1);
			var secondFrameOnePreviouseCandle = secondFrameCandles.ElementAtOrDefault(secondFrameCandles.Count - 2);

			if (secondFrameCurrentCandle?.VolumeInBaseCurrency == null ||
				secondFrameOnePreviouseCandle?.VolumeInBaseCurrency == null)
			{
				return conditionCheckingResult;
			}

			//if previouse volum lower then current 
			//if price changing direction from previouse candle to current
			if (!(secondFrameCurrentCandle.VolumeInBaseCurrency > secondFrameOnePreviouseCandle.VolumeInBaseCurrency &&
				  secondFrameOnePreviouseCandle.IsFallingCandle))
			{
				return conditionCheckingResult;
			}

			var secondFrameRSIValues = indicatorComputingService.ComputeRelativeStrengthIndex(
					secondFrameCandles,
					secondFrameRSISettings.Period)
				.OfType<SimpleIndicatorValue>()
				.ToList();

			var secondFrameCurrentRSIValue = secondFrameRSIValues.ElementAtOrDefault(secondFrameRSIValues.Count - 1);
			var secondFrameOnePreviouseRSIValue = secondFrameRSIValues.ElementAtOrDefault(secondFrameRSIValues.Count - 2);
			var secondFrameTwoPreviouseRSIValue = secondFrameRSIValues.ElementAtOrDefault(secondFrameRSIValues.Count - 3);

			if (secondFrameCurrentRSIValue?.Value == null ||
				secondFrameOnePreviouseRSIValue?.Value == null ||
				secondFrameTwoPreviouseRSIValue?.Value == null)
			{
				return conditionCheckingResult;
			}

			var secondFrameMaxRSIValue = secondFrameRSIValues
				.Where(value => value.Value.HasValue)
				.Select(value => value.Value.Value)
				.ToList()
				.GetMaximumValues()
				.Max();

			var lowRangeBroder = secondFrameMaxRSIValue * (isBullishTrendRising ? 0.8m : 0.67m);

			//if RSI turning from minimum 
			//if Prev RSI lower then lowRangeBroder
			if (!(Math.Round(secondFrameCurrentRSIValue.Value.Value - secondFrameOnePreviouseRSIValue.Value.Value) >= 0 &&
				  Math.Round(secondFrameTwoPreviouseRSIValue.Value.Value - secondFrameOnePreviouseRSIValue.Value.Value) >= 0 &&
				  Math.Round(secondFrameOnePreviouseRSIValue.Value.Value - lowRangeBroder) < 0 &&
				  Math.Round(secondFrameTwoPreviouseRSIValue.Value.Value - secondFrameMaxRSIValue) < 0))
			{
				return conditionCheckingResult;
			}

			if (!isBullishTrendRising)
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
					return conditionCheckingResult;
				}

				//If Histogram is growning
				if (Math.Round(secondFrameCurentMACDValue.Histogram.Value - secondFrameOnePreviouseMACDValue.Histogram.Value, 5) >= 0)
				{
					conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;
				}
				else if (secondFrameCurentMACDValue.MACD.Value > 0 &&
					secondFrameCurentMACDValue.MACD.Value > secondFrameCurentMACDValue.Signal.Value &&
					(secondFrameCurentMACDValue.MACD.Value - secondFrameCurentMACDValue.Signal.Value >
					 secondFrameOnePreviouseMACDValue.MACD.Value - secondFrameOnePreviouseMACDValue.Signal.Value ||
					 secondFrameOnePreviouseMACDValue.MACD.Value < secondFrameOnePreviouseMACDValue.Signal.Value))
				{
					conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;
				}
				else
					conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
			}
			else
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;

			return conditionCheckingResult;
		}
	}
}
