using System;
using System.Linq;
using System.Threading.Tasks;
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
	public class TripleFrameWilliamRStrategyAnalysisService : BaseNewPositionAnalysisService, IMarketNewPositionAnalysisService
	{
		public TripleFrameWilliamRStrategyAnalysisService(CandleLoadingService candleLoadingService,
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

					var candles = await CandleLoadingService.LoadCandles(
						currencyPair.Id,
						settings.Period,
						14,
						settings.Moment);

					var currentCandle = candles.Last();

					buyPositionInfo.OpenPrice = new[]
					{
						currentCandle.MaxPrice - currencyPair.TickSize * settings.LimitOrderPriceDifferneceFactor,
						currentCandle.MinPrice
					}.Max();
					buyPositionInfo.OpenStopPrice = currentCandle.MaxPrice;

					var lastPeakValue = IndicatorComputingService.ComputeHighestMaxPrices(
							candles,
							candles.Count)
						.OfType<SimpleIndicatorValue>()
						.LastOrDefault();

					buyPositionInfo.ClosePrice =
					buyPositionInfo.CloseStopPrice = lastPeakValue?.Value ?? candles.Max(candle => candle.MaxPrice);

					buyPositionInfo.StopLossPrice = candles.Min(candle => candle.MinPrice) -
													new[] { currencyPair.TickSize * settings.StopLossPriceDifferneceFactor, currentCandle.MaxPrice - currentCandle.MinPrice }.Max();

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

			var secondFrameWilliamsRSettings = new WilliamsRSettings
			{
				Period = 14 + WilliamsRSettings.MaxRangeFromLatestOppositePeak
			};

			var secondFrameCandles = (await CandleLoadingService.LoadCandles(
				currencyPair.Id,
				settings.Period,
				secondFrameWilliamsRSettings.Period + 1,
				settings.Moment)).ToList();

			var secondFrameCurrentCandle = secondFrameCandles.ElementAtOrDefault(secondFrameCandles.Count - 1);
			var secondFrameOnePreviousCandle = secondFrameCandles.ElementAtOrDefault(secondFrameCandles.Count - 2);

			if (secondFrameCurrentCandle?.VolumeInBaseCurrency == null ||
				secondFrameOnePreviousCandle?.VolumeInBaseCurrency == null)
			{
				return conditionCheckingResult;
			}

			////if previouse volume lower then current 
			////if price changing direction from previouse candle to current
			//if (!(secondFrameCurrentCandle.VolumeInBaseCurrency > secondFrameOnePreviouseCandle.VolumeInBaseCurrency &&
			//	  (secondFrameOnePreviouseCandle.IsFallingCandle || secondFrameCurrentCandle.OpenPrice < secondFrameOnePreviouseCandle.ClosePrice)))
			//{
			//	return conditionCheckingResult;
			//}

			var secondFrameWilliamsRValues = IndicatorComputingService.ComputeWilliamsR(
					secondFrameCandles,
					secondFrameWilliamsRSettings.Period)
				.OfType<SimpleIndicatorValue>()
				.ToList();

			var secondFrameRangeFromLastPeak = secondFrameWilliamsRValues
				.Where(value => value != null)
				.Skip(secondFrameWilliamsRValues.Count - WilliamsRSettings.MaxRangeFromLatestOppositePeak)
				.ToList();
			var secondFrameCurrentWilliamsRValue = secondFrameWilliamsRValues.ElementAtOrDefault(secondFrameWilliamsRValues.Count - 1);

			if (secondFrameCurrentWilliamsRValue?.Value == null)
			{
				return conditionCheckingResult;
			}

			if (secondFrameCurrentWilliamsRValue.Value < 90)
			{
				return conditionCheckingResult;
			}

			if (secondFrameRangeFromLastPeak.All(value => value.Value >= 90))
			{
				return conditionCheckingResult;
			}

			if (secondFrameRangeFromLastPeak.Max(value => value.Value) < WilliamsRSettings.MinHighPeakValue)
			{
				return conditionCheckingResult;
			}

			conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;
			return conditionCheckingResult;
		}
	}
}
