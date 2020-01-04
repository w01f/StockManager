using System.Data;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Common.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition
{
	public class OpenPositionAnalysisService : IMarketOpenPositionAnalysisService
	{
		private readonly CandleLoadingService _candleLoadingService;
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly IIndicatorComputingService _indicatorComputingService;
		private readonly ConfigurationService _configurationService;

		public OpenPositionAnalysisService(CandleLoadingService candleLoadingService,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService,
			ConfigurationService configurationService)
		{
			_candleLoadingService = candleLoadingService;
			_marketDataConnector = marketDataConnector;
			_indicatorComputingService = indicatorComputingService;
			_configurationService = configurationService;
		}

		public async Task<OpenPositionInfo> ProcessMarketPosition(OrderPair activeOrderPair)
		{
			var settings = _configurationService.GetTradingSettings();

			var initialPositionInfo = new UpdateClosePositionInfo
			{
				ClosePrice = activeOrderPair.ClosePositionOrder.Price,
				CloseStopPrice = activeOrderPair.ClosePositionOrder.StopPrice ?? 0,
				StopLossPrice = activeOrderPair.StopLossOrder.StopPrice ?? 0
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
					activeOrderPair.OpenPositionOrder.CurrencyPair.Id,
					settings.Period,
					candleRangeSize,
					settings.Moment))
				.ToList();

			if (!targetPeriodLastCandles.Any())
				throw new NoNullAllowedException("No candles loaded");
			var currentTargetPeriodCandle = targetPeriodLastCandles.Last();

			var higherPeriodLastCandles = (await _candleLoadingService.LoadCandles(
					activeOrderPair.OpenPositionOrder.CurrencyPair.Id,
					settings.Period.GetHigherFramePeriod(),
					williamsRSettings.Period,
					settings.Moment))
				.ToList();
			if (!higherPeriodLastCandles.Any())
				throw new NoNullAllowedException("No candles loaded");

			var lowerPeriodCandles = (await _candleLoadingService.LoadCandles(
					activeOrderPair.OpenPositionOrder.CurrencyPair.Id,
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
					activeOrderPair.StopLossOrder,
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
					currentWilliamsRValue?.Value > 5 &&
					currentWilliamsRValue.Value > previousWilliamsRValue?.Value) ||
				(activeOrderPair.ClosePositionOrder.OrderStateType != OrderStateType.Pending &&
					currentWilliamsRValue?.Value > 80 &&
					currentWilliamsRValue.Value > previousWilliamsRValue?.Value))
			{
				var updatePositionInfo = new UpdateClosePositionInfo { StopLossPrice = ((FixStopLossInfo)newPositionInfo)?.StopLossPrice ?? initialPositionInfo.StopLossPrice };

				var orderBookAskItems = (await _marketDataConnector.GetOrderBook(activeOrderPair.ClosePositionOrder.CurrencyPair.Id, 5))
					.Where(item => item.Type == OrderBookItemType.Ask)
					.ToList();

				if (!orderBookAskItems.Any())
					throw new NoNullAllowedException("Couldn't load order book");

				if (activeOrderPair.ClosePositionOrder.OrderStateType == OrderStateType.Pending ||
					activeOrderPair.ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
				{
					var maxAskSize = orderBookAskItems
						.Max(item => item.Size);

					var bottomMeaningfulAskPrice = orderBookAskItems
						.Where(item => item.Size == maxAskSize)
						.Select(item => item.Price)
						.First();

					updatePositionInfo.ClosePrice = bottomMeaningfulAskPrice - activeOrderPair.ClosePositionOrder.CurrencyPair.TickSize;

					var orderBookBidItems = (await _marketDataConnector.GetOrderBook(activeOrderPair.ClosePositionOrder.CurrencyPair.Id, 5))
						.Where(item => item.Type == OrderBookItemType.Bid)
						.ToList();

					if (!orderBookBidItems.Any())
						throw new NoNullAllowedException("Couldn't load order book");

					var topMeaningfulBidPrice = orderBookBidItems
						.OrderByDescending(item => item.Price)
						.Skip(1)
						.Select(item => item.Price)
						.First();

					updatePositionInfo.CloseStopPrice = new[] { activeOrderPair.ClosePositionOrder.StopPrice ?? 0, topMeaningfulBidPrice }.Max();
				}
				else
				{
					var bottomAskPrice = orderBookAskItems
						.OrderBy(item => item.Price)
						.Select(item => item.Price)
						.First();

					updatePositionInfo.ClosePrice = activeOrderPair.ClosePositionOrder.Price == bottomAskPrice ?
						bottomAskPrice :
						bottomAskPrice - activeOrderPair.ClosePositionOrder.CurrencyPair.TickSize;

					updatePositionInfo.CloseStopPrice = 0;
				}

				if (updatePositionInfo.ClosePrice != initialPositionInfo.ClosePrice ||
					updatePositionInfo.CloseStopPrice != initialPositionInfo.CloseStopPrice)
					newPositionInfo = updatePositionInfo;
			}
			else if (activeOrderPair.ClosePositionOrder.OrderStateType != OrderStateType.Pending &&
					currentWilliamsRValue?.Value > (useExtendedBorders ? 30 : 20) &&
					currentWilliamsRValue.Value < previousWilliamsRValue?.Value)
				newPositionInfo = new SuspendPositionInfo();
			else if (activeOrderPair.ClosePositionOrder.OrderStateType != OrderStateType.Pending &&
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
