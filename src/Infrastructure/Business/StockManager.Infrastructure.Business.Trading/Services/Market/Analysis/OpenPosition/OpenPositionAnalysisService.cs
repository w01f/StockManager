using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Entities.Market;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Common.Enums;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition
{
	public class OpenPositionAnalysisService : IMarketOpenPositionAnalysisService
	{
		private const int AccelerationFactorMaxPeriod = 10;

		private readonly IRepository<Candle> _candleRepository;
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly IIndicatorComputingService _indicatorComputingService;

		public OpenPositionAnalysisService(IRepository<Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService)
		{
			_candleRepository = candleRepository;
			_marketDataConnector = marketDataConnector;
			_indicatorComputingService = indicatorComputingService;
		}

		public async Task<OpenPositionInfo> ProcessMarketPosition(TradingSettings settings, OrderPair activeOrderPair)
		{
			var initialPositionInfo = new UpdateClosePositionInfo
			{
				ClosePrice = activeOrderPair.ClosePositionOrder.Price,
				CloseStopPrice = activeOrderPair.ClosePositionOrder.StopPrice ?? 0,
				StopLossPrice = activeOrderPair.StopLossOrder.Price
			};

			var newPositionInfo = new UpdateClosePositionInfo
			{
				ClosePrice = activeOrderPair.ClosePositionOrder.Price,
				CloseStopPrice = activeOrderPair.ClosePositionOrder.StopPrice ?? 0,
				StopLossPrice = activeOrderPair.StopLossOrder.Price
			};

			var williamsRSettings = new CommonIndicatorSettings
			{
				Period = 5
			};

			var candleRangeSize = new[] { williamsRSettings.Period, AccelerationFactorMaxPeriod }.Max();

			var targetPeriodLastCandles = (await CandleLoader.Load(
					settings.CurrencyPairId,
					settings.Period,
					candleRangeSize + 1,
					settings.Moment,
					_candleRepository,
					_marketDataConnector))
				.ToList();

			if (!targetPeriodLastCandles.Any())
				throw new NoNullAllowedException("No candles loaded");

			var currentCandle = targetPeriodLastCandles.FirstOrDefault(candle => candle.Moment == settings.Moment);

			if (activeOrderPair.ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
			{
				if (currentCandle != null)
				{
					newPositionInfo.StopLossPrice = ComputeParabolicSAR(targetPeriodLastCandles, newPositionInfo.StopLossPrice, settings);

					var williamsRValues = _indicatorComputingService.ComputeWilliamsR(
							targetPeriodLastCandles,
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
							currentCandle.MinPrice,
							activeOrderPair.ClosePositionOrder.StopPrice ?? 0,
							newPositionInfo.StopLossPrice + activeOrderPair.ClosePositionOrder.CurrencyPair.TickSize * settings.StopLimitPriceDifferneceFactor
						}.Max();
						newPositionInfo.ClosePrice = newPositionInfo.CloseStopPrice + activeOrderPair.ClosePositionOrder.CurrencyPair.TickSize * settings.StopLimitPriceDifferneceFactor;
					}
				}
				else
				{
					var lowerPeriodCandles = (await CandleLoader.Load(
							settings.CurrencyPairId,
							settings.Period.GetLowerFramePeriod(),
							williamsRSettings.Period + 1,
							settings.Moment,
							_candleRepository,
							_marketDataConnector))
						.ToList();

					if (!lowerPeriodCandles.Any())
						throw new NoNullAllowedException("No candles loaded");

					var currentLowPeriodCandle = lowerPeriodCandles.Last();

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

				var orderBookAskItems = (await _marketDataConnector.GetOrderBook(settings.CurrencyPairId, 20))
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
			   initialPositionInfo.CloseStopPrice == newPositionInfo.ClosePrice &&
			   initialPositionInfo.CloseStopPrice == newPositionInfo.StopLossPrice)
				return new HoldPositionInfo();

			return newPositionInfo;
		}

		private decimal ComputeParabolicSAR(
			IList<Infrastructure.Common.Models.Market.Candle> targetCandles, 
			decimal lastSar, 
			TradingSettings settings)
		{
			var candlesForComputing = targetCandles
				.Skip(targetCandles.Count - (AccelerationFactorMaxPeriod + 1))
				.Reverse()
				.ToList();

			if (!candlesForComputing.Any())
				throw new NoNullAllowedException("No candles loaded");

			var currentCandle = candlesForComputing.First();

			var accelerationFactorPeriod = 0;

			for (var i = 0; i < AccelerationFactorMaxPeriod; i++)
			{
				if (candlesForComputing[i].MinPrice <= lastSar)
					break;

				if (candlesForComputing[i].MinPrice > candlesForComputing[i + 1].MinPrice)
					accelerationFactorPeriod++;
				else
					break;
			}

			var sar = lastSar + settings.ParabolicSARBaseAccelerationFactror * accelerationFactorPeriod * (currentCandle.MaxPrice - lastSar);

			return sar;
		}
	}
}
