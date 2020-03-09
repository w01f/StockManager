using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Business.Trading.Services.Extensions.Connectors;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition
{
	public class OpenPositionAnalysisService : IMarketOpenPositionAnalysisService
	{
		private readonly CandleLoadingService _candleLoadingService;
		private readonly OrderBookLoadingService _orderBookLoadingService;
		private readonly IIndicatorComputingService _indicatorComputingService;
		private readonly ConfigurationService _configurationService;

		public OpenPositionAnalysisService(CandleLoadingService candleLoadingService,
			OrderBookLoadingService orderBookLoadingService,
			IIndicatorComputingService indicatorComputingService,
			ConfigurationService configurationService)
		{
			_candleLoadingService = candleLoadingService ?? throw new ArgumentNullException(nameof(candleLoadingService));
			_orderBookLoadingService = orderBookLoadingService ?? throw new ArgumentNullException(nameof(orderBookLoadingService));
			_indicatorComputingService = indicatorComputingService ?? throw new ArgumentNullException(nameof(indicatorComputingService));
			_configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
		}

		public async Task<OpenPositionInfo> ProcessMarketPosition(TradingPosition activeTradingPosition)
		{
			var settings = _configurationService.GetTradingSettings();

			var initialPositionInfo = new UpdateClosePositionInfo
			{
				ClosePrice = activeTradingPosition.ClosePositionOrder.Price,
				CloseStopPrice = activeTradingPosition.ClosePositionOrder.StopPrice ?? 0,
				StopLossPrice = activeTradingPosition.StopLossOrder.StopPrice ?? 0
			};

			OpenPositionInfo newPositionInfo = null;

			var williamsRSettings = new CommonIndicatorSettings
			{
				Period = 10
			};

			var higherPeriodMACDSettings = new MACDSettings
			{
				EMAPeriod1 = 12,
				EMAPeriod2 = 26,
				SignalPeriod = 9
			};

			var candleRangeSize = new[]
			{
				williamsRSettings.Period + 2,
				2
			}.Max();

			var targetPeriodLastCandles = (await _candleLoadingService.LoadCandles(
					activeTradingPosition.OpenPositionOrder.CurrencyPair.Id,
					settings.Period,
					candleRangeSize,
					settings.Moment))
				.ToList();

			if (!targetPeriodLastCandles.Any())
				throw new NoNullAllowedException("No candles loaded");
			var currentTargetPeriodCandle = targetPeriodLastCandles.Last();

			var higherPeriodLastCandles = (await _candleLoadingService.LoadCandles(
					activeTradingPosition.OpenPositionOrder.CurrencyPair.Id,
					settings.Period.GetHigherFramePeriod(),
					williamsRSettings.Period,
					settings.Moment))
				.ToList();
			if (!higherPeriodLastCandles.Any())
				throw new NoNullAllowedException("No candles loaded");

			var lowerPeriodCandles = (await _candleLoadingService.LoadCandles(
					activeTradingPosition.OpenPositionOrder.CurrencyPair.Id,
					settings.Period.GetLowerFramePeriod(),
					williamsRSettings.Period + 1,
					settings.Moment))
				.ToList();

			if (!lowerPeriodCandles.Any())
				throw new NoNullAllowedException("No candles loaded");
			var currentLowPeriodCandle = lowerPeriodCandles.Last();

			if (currentTargetPeriodCandle.Moment < currentLowPeriodCandle.Moment)
			{
				var lastLowPeriodCandles = lowerPeriodCandles
					.Where(item => item.Moment > currentTargetPeriodCandle.Moment)
					.OrderBy(item => item.Moment)
					.ToList();

				if (lastLowPeriodCandles.Any())
				{
					targetPeriodLastCandles.Add(new Candle
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
			else
			{
				var fixStopLossInfo = new FixStopLossInfo { StopLossPrice = initialPositionInfo.StopLossPrice };
				ComputeStopLossUsingParabolicSAR(
					fixStopLossInfo,
					activeTradingPosition.StopLossOrder,
					currentTargetPeriodCandle);

				if (fixStopLossInfo.StopLossPrice != initialPositionInfo.StopLossPrice)
					newPositionInfo = fixStopLossInfo;
			}

			var williamsRValues = _indicatorComputingService.ComputeWilliamsR(
					targetPeriodLastCandles,
					williamsRSettings.Period)
				.OfType<SimpleIndicatorValue>()
				.ToList();

			var currentWilliamsRValue = williamsRValues.ElementAtOrDefault(williamsRValues.Count - 1);
			var previousWilliamsRValue = williamsRValues.ElementAtOrDefault(williamsRValues.Count - 2);

			var higherPeriodMACDValues = _indicatorComputingService.ComputeMACD(
					higherPeriodLastCandles,
					higherPeriodMACDSettings.EMAPeriod1,
					higherPeriodMACDSettings.EMAPeriod2,
					higherPeriodMACDSettings.SignalPeriod)
				.OfType<MACDValue>()
				.ToList();

			var higherPeriodCurrentMACDValue = higherPeriodMACDValues.ElementAtOrDefault(higherPeriodMACDValues.Count - 1);
			var useExtendedBorders = higherPeriodCurrentMACDValue?.Histogram < 0;

			if ((currentWilliamsRValue?.Value <= (useExtendedBorders ? 30 : 20) &&
					currentWilliamsRValue.Value > 5 &&
					currentWilliamsRValue.Value > previousWilliamsRValue?.Value) ||
				(activeTradingPosition.ClosePositionOrder.OrderStateType != OrderStateType.Pending &&
					currentWilliamsRValue?.Value > 80 &&
					currentWilliamsRValue.Value > previousWilliamsRValue?.Value))
			{
				var updatePositionInfo = new UpdateClosePositionInfo
				{
					StopLossPrice = ((FixStopLossInfo)newPositionInfo)?.StopLossPrice ?? initialPositionInfo.StopLossPrice
				};

				if (activeTradingPosition.ClosePositionOrder.OrderStateType == OrderStateType.Pending ||
					activeTradingPosition.ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
				{
					var bottomMeaningfulAskPrice = await _orderBookLoadingService.GetBottomMeaningfulAskPrice(activeTradingPosition.ClosePositionOrder.CurrencyPair);

					updatePositionInfo.ClosePrice = bottomMeaningfulAskPrice - activeTradingPosition.ClosePositionOrder.CurrencyPair.TickSize;

					var topBidPrice = await _orderBookLoadingService.GetTopBidPrice(activeTradingPosition.ClosePositionOrder.CurrencyPair, 1);

					updatePositionInfo.CloseStopPrice = new[] { activeTradingPosition.ClosePositionOrder.StopPrice ?? 0, topBidPrice }.Max();
				}
				else
				{
					var bottomAskPrice = await _orderBookLoadingService.GetBottomAskPrice(activeTradingPosition.ClosePositionOrder.CurrencyPair);

					updatePositionInfo.ClosePrice = activeTradingPosition.ClosePositionOrder.Price == bottomAskPrice ?
						bottomAskPrice :
						bottomAskPrice - activeTradingPosition.ClosePositionOrder.CurrencyPair.TickSize;

					updatePositionInfo.CloseStopPrice = 0;
				}

				if (updatePositionInfo.ClosePrice != initialPositionInfo.ClosePrice ||
					updatePositionInfo.CloseStopPrice != initialPositionInfo.CloseStopPrice)
					newPositionInfo = updatePositionInfo;
			}
			else if (activeTradingPosition.ClosePositionOrder.OrderStateType != OrderStateType.Pending &&
					currentWilliamsRValue?.Value > (useExtendedBorders ? 30 : 20) &&
					currentWilliamsRValue.Value < previousWilliamsRValue?.Value)
				newPositionInfo = new SuspendPositionInfo();
			else if (activeTradingPosition.ClosePositionOrder.OrderStateType != OrderStateType.Pending &&
					currentWilliamsRValue?.Value <= 5)
				newPositionInfo = new SuspendPositionInfo();

			if (newPositionInfo == null)
				return new HoldPositionInfo();

			return newPositionInfo;
		}

		private void ComputeStopLossUsingParabolicSAR(
			FixStopLossInfo positionInfo,
			Order stopLossOrder,
			Candle currentCandle)
		{
			var settings = _configurationService.GetAnalysisSettings();

			if (stopLossOrder.AnalysisInfo == null)
			{
				stopLossOrder.AnalysisInfo = new StopLossOrderInfo
				{
					LastMaxValue = currentCandle.MaxPrice,
					TrailingStopAccelerationFactor = settings.ParabolicSARBaseAccelerationFactror
				};
			}
			else
			{
				var stopLossInfo = (StopLossOrderInfo)stopLossOrder.AnalysisInfo;
				if (currentCandle.MaxPrice > stopLossInfo.LastMaxValue)
				{
					stopLossInfo.LastMaxValue = currentCandle.MaxPrice;
					if (stopLossInfo.TrailingStopAccelerationFactor < settings.ParabolicSARMaxAccelerationFactror)
						stopLossInfo.TrailingStopAccelerationFactor += settings.ParabolicSARBaseAccelerationFactror;
				}

				positionInfo.StopLossPrice = (stopLossOrder.StopPrice +
					stopLossInfo.TrailingStopAccelerationFactor * (stopLossInfo.LastMaxValue - stopLossOrder.StopPrice)) ?? 0;
			}
		}
	}
}
