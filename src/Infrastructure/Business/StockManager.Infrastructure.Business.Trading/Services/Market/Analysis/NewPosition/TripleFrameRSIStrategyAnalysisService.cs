using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Business.Trading.Services.Extensions.Connectors;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition
{
	public class TripleFrameRSIStrategyAnalysisService : BaseNewPositionAnalysisService, IMarketNewPositionAnalysisService
	{
		public TripleFrameRSIStrategyAnalysisService(CandleLoadingService candleLoadingService,
			OrderBookLoadingService orderBookLoadingService,
			IIndicatorComputingService indicatorComputingService,
			ConfigurationService configurationService)
		{
			CandleLoadingService = candleLoadingService ?? throw new ArgumentNullException(nameof(candleLoadingService));
			OrderBookLoadingService = orderBookLoadingService ?? throw new ArgumentNullException(nameof(orderBookLoadingService));
			IndicatorComputingService = indicatorComputingService ?? throw new ArgumentNullException(nameof(indicatorComputingService));
			ConfigurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
		}

		public async Task<NewPositionInfo> ProcessMarketPosition(CurrencyPair currencyPair)
		{
			NewPositionInfo newPositionInfo;
			var conditionCheckingResult = await CheckConditions(currencyPair);

			switch (conditionCheckingResult.ResultType)
			{
				case ConditionCheckingResultType.Passed:
					var buyPositionInfo = new NewOrderPositionInfo(NewMarketPositionType.Buy);
					buyPositionInfo.CurrencyPairId = currencyPair.Id;

					buyPositionInfo.OpenPrice = await OrderBookLoadingService.GetTopMeaningfulBidPrice(currencyPair);

					buyPositionInfo.OpenStopPrice = await OrderBookLoadingService.GetBottomAskPrice(currencyPair, 1);

					buyPositionInfo.ClosePrice =
					buyPositionInfo.CloseStopPrice =
						buyPositionInfo.OpenStopPrice;

					buyPositionInfo.StopLossPrice = buyPositionInfo.OpenPrice - currencyPair.TickSize * 2000;

					newPositionInfo = buyPositionInfo;
					break;
				default:
					newPositionInfo = new WaitPositionInfo();
					break;
			}

			return newPositionInfo;
		}

		protected override async Task<ConditionCheckingResult> CheckConditions(CurrencyPair currencyPair)
		{
			var settings = ConfigurationService.GetTradingSettings();
			var moment = settings.Moment ?? DateTime.UtcNow;

			var conditionCheckingResult = new ConditionCheckingResult() { ResultType = ConditionCheckingResultType.Failed };

			var firstFrameMACDSettings = new MACDSettings
			{
				EMAPeriod1 = 12,
				EMAPeriod2 = 26,
				SignalPeriod = 9
			};

			var firstFrameCandles = (await CandleLoadingService.LoadCandles(
				currencyPair.Id,
				settings.Period.GetHigherFramePeriod(),
				firstFrameMACDSettings.RequiredCandleRangeSize,
				moment))
				.OrderBy(candle => candle.Moment)
				.ToList();

			var firstFrameMACDValues = IndicatorComputingService.ComputeMACD(
					firstFrameCandles,
					firstFrameMACDSettings.EMAPeriod1,
					firstFrameMACDSettings.EMAPeriod2,
					firstFrameMACDSettings.SignalPeriod)
				.OfType<MACDValue>()
				.ToList();

			var firstFrameCurrentMACDValue = firstFrameMACDValues.ElementAtOrDefault(firstFrameMACDValues.Count - 1);
			var firstFrameOnePreviousMACDValue = firstFrameMACDValues.ElementAtOrDefault(firstFrameMACDValues.Count - 2);

			//If all valuable parameters are not null
			if (firstFrameCurrentMACDValue?.Histogram == null ||
				firstFrameOnePreviousMACDValue?.Histogram == null)
			{
				return conditionCheckingResult;
			}

			//if MACD higher then Signal then it is Bullish trend
			//if Histogram is rising 
			if (!(firstFrameCurrentMACDValue.MACD > 0 ||
				firstFrameCurrentMACDValue.Histogram.Value >= 0 ||
				firstFrameCurrentMACDValue.Histogram.Value > firstFrameOnePreviousMACDValue.Histogram))
			{
				return conditionCheckingResult;
			}

			var rsiSettings = new RSISettings
			{
				Period = 14
			};

			var secondFrameTargetPeriodCandles = (await CandleLoadingService.LoadCandles(
				currencyPair.Id,
				settings.Period,
				rsiSettings.Period + 2,
				moment))
				.OrderBy(candle => candle.Moment)
				.ToList();

			var candlesCount = secondFrameTargetPeriodCandles.Count;
			if (candlesCount < rsiSettings.Period)
				return conditionCheckingResult;

			var secondFrameCurrentCandle = secondFrameTargetPeriodCandles.ElementAtOrDefault(secondFrameTargetPeriodCandles.Count - 1);
			if (secondFrameCurrentCandle?.VolumeInBaseCurrency == null)
				return conditionCheckingResult;

			var lowerPeriodCandles = (await CandleLoadingService.LoadCandles(
					currencyPair.Id,
					settings.Period.GetLowerFramePeriod(),
					rsiSettings.Period,
					moment))
				.OrderBy(candle => candle.Moment)
				.ToList();

			if (!lowerPeriodCandles.Any())
				throw new NoNullAllowedException("No candles loaded");
			var currentLowPeriodCandle = lowerPeriodCandles.Last();

			if (secondFrameCurrentCandle.Moment != currentLowPeriodCandle.Moment)
			{
				var lastLowPeriodCandles = lowerPeriodCandles
					.Where(item => item.Moment > secondFrameCurrentCandle.Moment)
					.OrderBy(item => item.Moment)
					.ToList();

				if (lastLowPeriodCandles.Any())
				{
					secondFrameTargetPeriodCandles.Add(new Candle
					{
						Moment = lastLowPeriodCandles.Last().Moment,
						MaxPrice = lastLowPeriodCandles.Max(item => item.MaxPrice),
						MinPrice = lastLowPeriodCandles.Min(item => item.MinPrice),
						OpenPrice = lastLowPeriodCandles.First().OpenPrice,
						ClosePrice = lastLowPeriodCandles.Last().ClosePrice,
						VolumeInBaseCurrency = lastLowPeriodCandles.Sum(item => item.VolumeInBaseCurrency),
						VolumeInQuoteCurrency = lastLowPeriodCandles.Sum(item => item.VolumeInQuoteCurrency)
					});
				}
			}
			
			var period = (candlesCount - 2) > rsiSettings.Period ? rsiSettings.Period : candlesCount - 2;
			var secondFrameRSIValues = IndicatorComputingService.ComputeRelativeStrengthIndex(
					secondFrameTargetPeriodCandles,
					period)
				.OfType<SimpleIndicatorValue>()
				.ToList();

			var secondFrameCurrentRSIValue = secondFrameRSIValues.ElementAtOrDefault(secondFrameRSIValues.Count - 1);
			var secondFrameOnePreviousRSIValue = secondFrameRSIValues.ElementAtOrDefault(secondFrameRSIValues.Count - 2);

			if (secondFrameCurrentRSIValue?.Value == null || secondFrameOnePreviousRSIValue?.Value == null)
			{
				return conditionCheckingResult;
			}

			var rsiTopBorder = 45;
			if (firstFrameCurrentMACDValue.MACD > 0 && firstFrameCurrentMACDValue.Histogram.Value >= 0)
				rsiTopBorder = 65;
			if (firstFrameCurrentMACDValue.MACD > 0 || firstFrameCurrentMACDValue.Histogram.Value >= 0)
				rsiTopBorder = 60;
			if (secondFrameCurrentRSIValue.Value > rsiTopBorder || secondFrameCurrentRSIValue.Value < 25)
			{
				return conditionCheckingResult;
			}

			if (secondFrameCurrentRSIValue.Value < secondFrameOnePreviousRSIValue.Value)
			{
				return conditionCheckingResult;
			}

			conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;
			return conditionCheckingResult;
		}
	}
}
