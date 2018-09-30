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

			var newPositionInfo = new UpdateClosePositionInfo
			{
				ClosePrice = activeOrderPair.ClosePositionOrder.Price,
				CloseStopPrice = activeOrderPair.ClosePositionOrder.StopPrice ?? 0,
				StopLossPrice = activeOrderPair.StopLossOrder.StopPrice ?? 0
			};

			var williamsRSettings = new CommonIndicatorSettings
			{
				Period = 5
			};

			var candleRangeSize = new[]
			{
				williamsRSettings.Period+1,
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

			var lowerPeriodCandles = (await _candleLoadingService.LoadCandles(
					activeOrderPair.OpenPositionOrder.CurrencyPair.Id,
					settings.Period.GetLowerFramePeriod(),
					williamsRSettings.Period + 1,
					settings.Moment))
				.ToList();

			if (!lowerPeriodCandles.Any())
				throw new NoNullAllowedException("No candles loaded");
			var currentLowPeriodCandle = lowerPeriodCandles.Last();

			if (activeOrderPair.ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
			{
				if (currentTargetPeriodCandle.Moment == currentLowPeriodCandle.Moment)
				{
					ComputeStopLossUsingParabolicSAR(
						activeOrderPair.StopLossOrder,
						currentTargetPeriodCandle);

					var williamsRValues = _indicatorComputingService.ComputeWilliamsR(
							targetPeriodLastCandles,
							williamsRSettings.Period)
						.OfType<SimpleIndicatorValue>()
						.ToList();

					var currentWilliamsRValue = williamsRValues.ElementAtOrDefault(williamsRValues.Count - 1);

					if (currentWilliamsRValue == null)
					{
						throw new NoNullAllowedException("No WilliamR values calculated");
					}

					if (currentWilliamsRValue.Value <= 10)
					{
						newPositionInfo.CloseStopPrice = new[]
						{
							currentTargetPeriodCandle.MinPrice,
							activeOrderPair.ClosePositionOrder.StopPrice ?? 0,
							newPositionInfo.StopLossPrice + activeOrderPair.ClosePositionOrder.CurrencyPair.TickSize * settings.StopLimitPriceDifferneceFactor
						}.Max();
						newPositionInfo.ClosePrice = newPositionInfo.CloseStopPrice + activeOrderPair.ClosePositionOrder.CurrencyPair.TickSize * settings.StopLimitPriceDifferneceFactor;
					}
				}
				else
				{
					var williamsRValues = _indicatorComputingService.ComputeWilliamsR(
							lowerPeriodCandles,
							williamsRSettings.Period)
						.OfType<SimpleIndicatorValue>()
						.ToList();

					var currentWilliamsRValue = williamsRValues.ElementAtOrDefault(williamsRValues.Count - 1);

					if (currentWilliamsRValue?.Value == null)
					{
						throw new NoNullAllowedException("No WilliamR values calculated");
					}

					if (currentWilliamsRValue.Value <= 10)
					{
						newPositionInfo.CloseStopPrice = new[]
						{
							currentLowPeriodCandle.MinPrice,
							activeOrderPair.ClosePositionOrder.StopPrice ?? 0
						}.Max();
						newPositionInfo.ClosePrice = newPositionInfo.CloseStopPrice - activeOrderPair.ClosePositionOrder.CurrencyPair.TickSize * settings.StopLimitPriceDifferneceFactor;
					}
				}
			}
			else
			{
				newPositionInfo.CloseStopPrice = 0;

				var orderBookAskItems = (await _marketDataConnector.GetOrderBook(activeOrderPair.OpenPositionOrder.CurrencyPair.Id, 20))
					.Where(item => item.Type == OrderBookItemType.Ask)
					.ToList();

				if (!orderBookAskItems.Any())
					throw new NoNullAllowedException("Couldn't load order book");

				var avgAskSize = orderBookAskItems
					.Average(item => item.Size);

				var bottomAskPrice = orderBookAskItems
					.OrderBy(item => item.Price)
					.Select(item => item.Price)
					.First();

				var nearestAskSupportPrice = orderBookAskItems
					.Where(item => item.Size > avgAskSize && item.Price > bottomAskPrice)
					.OrderBy(item => item.Price)
					.Select(item => item.Price)
					.First();

				newPositionInfo.ClosePrice = new[]
				{
					nearestAskSupportPrice - activeOrderPair.ClosePositionOrder.CurrencyPair.TickSize,
					activeOrderPair.ClosePositionOrder.Price
				}.Min();
			}

			if (initialPositionInfo.CloseStopPrice == newPositionInfo.CloseStopPrice &&
			   initialPositionInfo.ClosePrice == newPositionInfo.ClosePrice &&
			   initialPositionInfo.StopLossPrice == newPositionInfo.StopLossPrice)
				return new HoldPositionInfo();

			return newPositionInfo;
		}

		private void ComputeStopLossUsingParabolicSAR(
			Order stopLossOrder,
			Common.Models.Market.Candle currentCandle)
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

				stopLossOrder.StopPrice = stopLossOrder.StopPrice +
					stopLossInfo.TrailingStopAccelerationFactor * (stopLossInfo.LastMaxValue - stopLossOrder.StopPrice);
			}
		}
	}
}
