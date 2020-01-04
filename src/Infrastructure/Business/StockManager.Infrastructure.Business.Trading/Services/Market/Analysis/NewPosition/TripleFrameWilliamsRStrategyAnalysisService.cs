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
using StockManager.Infrastructure.Common.Enums;
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

					var orderBookBidItems = (await MarketDataConnector.GetOrderBook(currencyPair.Id, 5))
						.Where(item => item.Type == OrderBookItemType.Bid)
						.ToList();

					if (!orderBookBidItems.Any())
						throw new NoNullAllowedException("Couldn't load order book");

					var maxBidSize = orderBookBidItems
						.Max(item => item.Size);

					var topMeaningfulBidPrice = orderBookBidItems
						.Where(item => item.Size == maxBidSize)
						.OrderByDescending(item => item.Price)
						.Select(item => item.Price)
						.First();

					buyPositionInfo.OpenPrice = topMeaningfulBidPrice;

					var orderBookAskItems = (await MarketDataConnector.GetOrderBook(currencyPair.Id, 5))
						.Where(item => item.Type == OrderBookItemType.Ask)
						.ToList();

					if (!orderBookAskItems.Any())
						throw new NoNullAllowedException("Couldn't load order book");

					var bottomMeaningfulAskPrice = orderBookAskItems
						.OrderBy(item => item.Price)
						.Skip(1)
						.Select(item => item.Price)
						.First();

					buyPositionInfo.OpenStopPrice = bottomMeaningfulAskPrice;

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

			var firstFrameCandles = (await CandleLoadingService.LoadCandles(
				currencyPair.Id,
				settings.Period.GetHigherFramePeriod(),
				firstFrameMACDSettings.RequiredCandleRangeSize,
				settings.Moment)).ToList();

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
			if (!(firstFrameCurrentMACDValue.Histogram.Value >= 0 ||
				firstFrameCurrentMACDValue.Histogram.Value > firstFrameOnePreviousMACDValue.Histogram))
			{
				return conditionCheckingResult;
			}

			var useExtendedBorders = firstFrameCurrentMACDValue.Histogram.Value >= 0;

			var secondFrameWilliamsRSettings = new WilliamsRSettings
			{
				Period = 14
			};

			var secondFrameTargetPeriodCandles = (await CandleLoadingService.LoadCandles(
				currencyPair.Id,
				settings.Period,
				secondFrameWilliamsRSettings.Period + 2,
				settings.Moment)).ToList();

			var secondFrameCurrentCandle = secondFrameTargetPeriodCandles.ElementAtOrDefault(secondFrameTargetPeriodCandles.Count - 1);
			if (secondFrameCurrentCandle?.VolumeInBaseCurrency == null)
				return conditionCheckingResult;

			var lowerPeriodCandles = (await CandleLoadingService.LoadCandles(
					currencyPair.Id,
					settings.Period.GetLowerFramePeriod(),
					secondFrameWilliamsRSettings.Period,
					settings.Moment))
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

			var secondFrameWilliamsRValues = IndicatorComputingService.ComputeWilliamsR(
					secondFrameTargetPeriodCandles,
					secondFrameWilliamsRSettings.Period)
				.OfType<SimpleIndicatorValue>()
				.ToList();

			var secondFrameCurrentWilliamsRValue = secondFrameWilliamsRValues.ElementAtOrDefault(secondFrameWilliamsRValues.Count - 1);
			var secondFrameOnePreviousWilliamsRValue = secondFrameWilliamsRValues.ElementAtOrDefault(secondFrameWilliamsRValues.Count - 2);

			if (secondFrameCurrentWilliamsRValue?.Value == null || secondFrameOnePreviousWilliamsRValue?.Value == null)
			{
				return conditionCheckingResult;
			}

			if (secondFrameCurrentWilliamsRValue.Value < (useExtendedBorders ? 50 : 80) || secondFrameCurrentWilliamsRValue.Value > 95)
			{
				return conditionCheckingResult;
			}

			if (secondFrameCurrentWilliamsRValue.Value > secondFrameOnePreviousWilliamsRValue.Value)
			{
				return conditionCheckingResult;
			}

			conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;
			return conditionCheckingResult;
		}
	}
}
