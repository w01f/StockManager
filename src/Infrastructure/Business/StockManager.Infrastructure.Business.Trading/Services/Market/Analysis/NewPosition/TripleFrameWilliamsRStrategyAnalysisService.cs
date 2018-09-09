using System;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Entities.Market;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition
{
	public class TripleFrameWilliamRStrategyAnalysisService : BaseNewPositionAnalysisService, IMarketNewPositionAnalysisService
	{
		public TripleFrameWilliamRStrategyAnalysisService(IRepository<Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService)
		{
			CandleRepository = candleRepository;
			MarketDataConnector = marketDataConnector;
			IndicatorComputingService = indicatorComputingService;
		}

		public async Task<NewPositionInfo> ProcessMarketPosition(TradingSettings settings)
		{
			NewPositionInfo newPositionInfo;
			var conditionCheckingResult = await CheckConditions(settings);

			switch (conditionCheckingResult.ResultType)
			{
				case ConditionCheckingResultType.Passed:
					var buyPositionInfo = new NewOrderPositionInfo(NewMarketPositionType.Buy);

					var candles = await CandleLoader.Load(
						settings.CurrencyPairId,
						settings.Period,
						settings.CandleRangeSize,
						settings.Moment,
						CandleRepository,
						MarketDataConnector);

					var currentCandle = candles.Last();

					var currencyPair = await MarketDataConnector.GetCurrensyPair(settings.CurrencyPairId);

					buyPositionInfo.OpenPrice = currentCandle.MaxPrice - currencyPair.TickSize * settings.StopLimitPriceDifferneceFactor;
					buyPositionInfo.OpenStopPrice = currentCandle.MaxPrice;

					buyPositionInfo.ClosePrice =
					buyPositionInfo.CloseStopPrice = candles.Max(candle => candle.MaxPrice);

					buyPositionInfo.StopLossPrice = candles.Min(candle => candle.MinPrice);

					newPositionInfo = buyPositionInfo;
					break;
				default:
					newPositionInfo = new WaitPositionInfo();
					break;
			}

			return newPositionInfo;
		}

		//TODO Try to extract logical steps into separate objects
		protected override async Task<ConditionCheckingResult> CheckConditions(TradingSettings settings)
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
				settings.Moment,
				CandleRepository,
				MarketDataConnector)).ToList();

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

			var secondFrameWilliamsRSettings = new CommonIndicatorSettings
			{
				Period = 5
			};

			var secondFrameCandles = (await CandleLoader.Load(
				settings.CurrencyPairId,
				settings.Period,
				secondFrameWilliamsRSettings.Period + 1,
				settings.Moment,
				CandleRepository,
				MarketDataConnector)).ToList();

			var secondFrameCurrentCandle = secondFrameCandles.ElementAtOrDefault(secondFrameCandles.Count - 1);
			var secondFrameOnePreviouseCandle = secondFrameCandles.ElementAtOrDefault(secondFrameCandles.Count - 2);

			if (secondFrameCurrentCandle?.VolumeInBaseCurrency == null ||
				secondFrameOnePreviouseCandle?.VolumeInBaseCurrency == null)
			{
				return conditionCheckingResult;
			}

			//if previouse volume lower then current 
			//if price changing direction from previouse candle to current
			if (!(secondFrameCurrentCandle.VolumeInBaseCurrency > secondFrameOnePreviouseCandle.VolumeInBaseCurrency &&
				  (secondFrameOnePreviouseCandle.IsFallingCandle || secondFrameCurrentCandle.OpenPrice < secondFrameOnePreviouseCandle.ClosePrice)))
			{
				return conditionCheckingResult;
			}

			var secondFrameWilliamsRValues = IndicatorComputingService.ComputeWilliamsR(
					secondFrameCandles,
					secondFrameWilliamsRSettings.Period)
				.OfType<SimpleIndicatorValue>()
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

			conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;
			return conditionCheckingResult;
		}
	}
}
