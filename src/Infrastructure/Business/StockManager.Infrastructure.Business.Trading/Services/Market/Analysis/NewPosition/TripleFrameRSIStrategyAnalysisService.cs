using System;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Infrastructure.Analysis.Common.Helpers;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition
{
	public class TripleFrameRSIStrategyAnalysisService : BaseNewPositionAnalysisService, IMarketNewPositionAnalysisService
	{
		public TripleFrameRSIStrategyAnalysisService(CandleLoadingService candleLoadingService,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService,
			ConfigurationService configurationService)
		{
			CandleLoadingService = candleLoadingService;
			MarketDataConnector = marketDataConnector;
			IndicatorComputingService = indicatorComputingService;
			ConfigurationService = configurationService;
		}

		public async Task<NewPositionInfo> ProcessMarketPosition(CurrencyPair currencyPair)
		{
			var settings = ConfigurationService.GetTradingSettings();
			NewPositionInfo newPositionInfo;
			var conditionCheckingResult = await CheckConditions(currencyPair);

			switch (conditionCheckingResult.ResultType)
			{
				case ConditionCheckingResultType.Passed:
					var buyPositionInfo = new NewOrderPositionInfo(NewMarketPositionType.Buy);
					buyPositionInfo.CurrencyPairId = currencyPair.Id;

					var candles = (await CandleLoadingService.LoadCandles(
						currencyPair.Id,
						settings.Period,
						2,
						settings.Moment)).ToList();

					//TODO Define stop prices
					buyPositionInfo.OpenPrice = candles.Max(candle => candle.MaxPrice);
					buyPositionInfo.OpenStopPrice = candles.Max(candle => candle.MaxPrice);

					buyPositionInfo.ClosePrice = candles.Min(candle => candle.MinPrice);

					newPositionInfo = buyPositionInfo;
					break;
				default:
					newPositionInfo = new WaitPositionInfo();
					break;
			}

			return newPositionInfo;
		}

		//TODO Try to extract logical steps into separate objects
		protected override async Task<ConditionCheckingResult> CheckConditions(CurrencyPair currencyPair)
		{
			var settings = ConfigurationService.GetTradingSettings();

			var conditionCheckingResult = new ConditionCheckingResult() { ResultType = ConditionCheckingResultType.Failed };

			var firstFrameMACDSettings = new MACDSettings
			{
				EMAPeriod1 = 12,
				EMAPeriod2 = 26,
				SignalPeriod = 9
			};

			var firtsFrameCandles = (await CandleLoadingService.LoadCandles(
				currencyPair.Id,
				settings.Period.GetHigherFramePeriod(),
				firstFrameMACDSettings.RequiredCandleRangeSize,
				settings.Moment)).ToList();

			var firstFrameMACDValues = IndicatorComputingService.ComputeMACD(
					firtsFrameCandles,
					firstFrameMACDSettings.EMAPeriod1,
					firstFrameMACDSettings.EMAPeriod2,
					firstFrameMACDSettings.SignalPeriod)
				.OfType<MACDValue>()
				.ToList();

			var firstFrameCurrentMACDValue = firstFrameMACDValues.ElementAtOrDefault(firstFrameMACDValues.Count - 1);
			var firstFrameOnePreviouseMACDValue = firstFrameMACDValues.ElementAtOrDefault(firstFrameMACDValues.Count - 2);

			//If all valuable parameters are not null
			if (firstFrameCurrentMACDValue?.Histogram == null ||
				firstFrameOnePreviouseMACDValue?.Histogram == null)
			{
				return conditionCheckingResult;
			}

			//if MACD higher then Signal then it is Bullish trend
			//if Histogram is rising 
			if (!(Math.Round(firstFrameCurrentMACDValue.Histogram.Value, 5) >= 0))
			{
				return conditionCheckingResult;
			}

			var isBullishTrendRising = Math.Round(firstFrameCurrentMACDValue.Histogram.Value - firstFrameOnePreviouseMACDValue.Histogram.Value, 5) >= 0;
			var isBullishTrendJustBeganRising = isBullishTrendRising &&
												Math.Round(firstFrameOnePreviouseMACDValue.Histogram.Value, 4) < 0;

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

			var secondFrameCandles = (await CandleLoadingService.LoadCandles(
				currencyPair.Id,
				settings.Period,
				candleRangeSize,
				settings.Moment)).ToList();

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
				  (secondFrameOnePreviouseCandle.IsFallingCandle || secondFrameCurrentCandle.OpenPrice < secondFrameOnePreviouseCandle.ClosePrice)))
			{
				return conditionCheckingResult;
			}

			var secondFrameRSIValues = IndicatorComputingService.ComputeRelativeStrengthIndex(
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

			if (!isBullishTrendJustBeganRising)
			{
				var secondFrameMACDValues = IndicatorComputingService.ComputeMACD(
						secondFrameCandles,
						secondFrameMACDSettings.EMAPeriod1,
						secondFrameMACDSettings.EMAPeriod2,
						secondFrameMACDSettings.SignalPeriod)
					.OfType<MACDValue>()
					.ToList();

				var secondFrameCurentMACDValue = secondFrameMACDValues.ElementAtOrDefault(secondFrameMACDValues.Count - 1);
				var secondFrameOnePreviouseMACDValue = secondFrameMACDValues.ElementAtOrDefault(secondFrameMACDValues.Count - 2);

				//If all valuable parameters are not null
				if (secondFrameCurentMACDValue?.MACD == null ||
					secondFrameCurentMACDValue.Histogram == null ||
					secondFrameOnePreviouseMACDValue?.Histogram == null)
				{
					return conditionCheckingResult;
				}

				//If Histogram is growning
				if (Math.Round(secondFrameCurentMACDValue.Histogram.Value - secondFrameOnePreviouseMACDValue.Histogram.Value, 6) >= 0)
				{
					conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;
				}
				else if (secondFrameCurentMACDValue.MACD.Value > 0)
				{
					conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;
				}
				else
					conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;

				if (conditionCheckingResult.ResultType == ConditionCheckingResultType.Passed)
				{
					var thirdFrameMACDSettings = new MACDSettings
					{
						EMAPeriod1 = 12,
						EMAPeriod2 = 26,
						SignalPeriod = 9
					};

					var thirdFrameCandles = (await CandleLoadingService.LoadCandles(
						currencyPair.Id,
						settings.Period.GetLowerFramePeriod(),
						thirdFrameMACDSettings.EMAPeriod2,
						settings.Moment)).ToList();

					var thirdFrameMACDValues = IndicatorComputingService.ComputeMACD(
							thirdFrameCandles,
							thirdFrameMACDSettings.EMAPeriod1,
							thirdFrameMACDSettings.EMAPeriod2,
							thirdFrameMACDSettings.SignalPeriod)
						.OfType<MACDValue>()
						.ToList();

					var thirdFrameCurentMACDValue = thirdFrameMACDValues.ElementAtOrDefault(thirdFrameMACDValues.Count - 1);
					var thirdFrameOnePreviouseMACDValue = thirdFrameMACDValues.ElementAtOrDefault(thirdFrameMACDValues.Count - 2);

					//If Histogram is growning
					if (!(Math.Round((thirdFrameCurentMACDValue?.Histogram - thirdFrameOnePreviouseMACDValue?.Histogram) ?? -1, 5) >= 0))
					{
						conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
					}
				}
			}
			else
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;

			return conditionCheckingResult;
		}
	}
}
