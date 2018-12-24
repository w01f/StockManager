using System.Data;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Common.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition
{
	public class PendingPositionWilliamsRAnalysisService : IMarketPendingPositionAnalysisService
	{
		private readonly CandleLoadingService _candleLoadingService;
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly IIndicatorComputingService _indicatorComputingService;
		private readonly ConfigurationService _configurationService;

		public PendingPositionWilliamsRAnalysisService(CandleLoadingService candleLoadingService,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService,
			ConfigurationService configurationService)
		{
			_candleLoadingService = candleLoadingService;
			_marketDataConnector = marketDataConnector;
			_indicatorComputingService = indicatorComputingService;
			_configurationService = configurationService;
		}

		public async Task<PendingPositionInfo> ProcessMarketPosition(OrderPair activeOrderPair)
		{
			var settings = _configurationService.GetTradingSettings();

			var williamsRSettings = new CommonIndicatorSettings
			{
				Period = 14
			};

			var targetPeriodLastCandles = (await _candleLoadingService.LoadCandles(
				activeOrderPair.OpenPositionOrder.CurrencyPair.Id,
				settings.Period,
				williamsRSettings.Period + 1,
				settings.Moment))
				.ToList();

			if (!targetPeriodLastCandles.Any())
				throw new NoNullAllowedException("No candles loaded");
			var currentTargetCandle = targetPeriodLastCandles.Last();

			var lowerPeriodCandles = (await _candleLoadingService.LoadCandles(
					activeOrderPair.OpenPositionOrder.CurrencyPair.Id,
					settings.Period.GetLowerFramePeriod(),
					williamsRSettings.Period + 1,
					settings.Moment))
				.ToList();

			if (!lowerPeriodCandles.Any())
				throw new NoNullAllowedException("No candles loaded");

			var currentLowPeriodCandle = lowerPeriodCandles.Last();

			if (currentTargetCandle.Moment == currentLowPeriodCandle.Moment)
			{
				var williamsRValues = _indicatorComputingService.ComputeWilliamsR(
						targetPeriodLastCandles,
						williamsRSettings.Period)
					.OfType<SimpleIndicatorValue>()
					.ToList();

				var rangeFromLastPeak = williamsRValues
					.Where(value => value != null)
					.Skip(williamsRValues.Count - WilliamsRSettings.MaxRangeFromLatestOppositePeak)
					.ToList();
				var currentWilliamsRValue = williamsRValues.ElementAtOrDefault(williamsRValues.Count - 1);

				if (currentWilliamsRValue?.Value == null)
					throw new NoNullAllowedException("No WilliamR values calculated");

				if (currentWilliamsRValue.Value >= 90 &&
					rangeFromLastPeak.Any(value => value.Value < 90) &&
					rangeFromLastPeak.Max(value => value.Value) >= WilliamsRSettings.MinHighPeakValue)
				{
					decimal stopOpenPrice;
					decimal openPrice;
					if (activeOrderPair.OpenPositionOrder.OrderStateType == OrderStateType.Suspended)
					{
						stopOpenPrice = new[]
						{
								currentTargetCandle.MaxPrice,
								activeOrderPair.OpenPositionOrder.StopPrice ?? 0
							}.Min();
						openPrice = stopOpenPrice - activeOrderPair.OpenPositionOrder.CurrencyPair.TickSize * settings.LimitOrderPriceDifferneceFactor;
					}
					else
					{
						stopOpenPrice = 0;

						var nearestBidSupportPrice = await GetNearestBidSupportPrice(activeOrderPair.OpenPositionOrder.CurrencyPair);
						openPrice = new[]
						{
								nearestBidSupportPrice + activeOrderPair.OpenPositionOrder.CurrencyPair.TickSize,
								activeOrderPair.OpenPositionOrder.Price
							}.Max();
					}

					return new UpdateOrderInfo
					{
						OpenPrice = openPrice,
						OpenStopPrice = stopOpenPrice,

						ClosePrice = new[] { currentTargetCandle.MaxPrice, activeOrderPair.ClosePositionOrder.Price }.Max(),
						CloseStopPrice = new[] { currentTargetCandle.MinPrice, activeOrderPair.ClosePositionOrder.StopPrice ?? 0 }.Min(),

						StopLossPrice = new[]
						{
							currentTargetCandle.MinPrice - new[]{activeOrderPair.OpenPositionOrder.CurrencyPair.TickSize * settings.StopLossPriceDifferneceFactor, currentTargetCandle.MaxPrice-currentTargetCandle.MinPrice}.Max(),
							activeOrderPair.StopLossOrder.StopPrice ?? 0
						}.Min()
					};
				}
				return new CancelOrderInfo();
			}
			else if (activeOrderPair.OpenPositionOrder.OrderStateType != OrderStateType.Suspended)
			{
				decimal stopOpenPrice = 0;
				var nearestBidSupportPrice = await GetNearestBidSupportPrice(activeOrderPair.OpenPositionOrder.CurrencyPair);
				var openPrice = new[]
				{
					nearestBidSupportPrice + activeOrderPair.OpenPositionOrder.CurrencyPair.TickSize,
					activeOrderPair.OpenPositionOrder.Price
				}.Max();

				if (activeOrderPair.OpenPositionOrder.Price != openPrice)
					return new UpdateOrderInfo
					{
						OpenPrice = openPrice,
						OpenStopPrice = stopOpenPrice,

						ClosePrice = new[] { currentLowPeriodCandle.MaxPrice, activeOrderPair.ClosePositionOrder.Price }.Max(),
						CloseStopPrice = new[] { currentLowPeriodCandle.MinPrice, activeOrderPair.ClosePositionOrder.StopPrice ?? 0 }.Min(),

						StopLossPrice = new[]
						{
							currentLowPeriodCandle.MinPrice - new[]{activeOrderPair.OpenPositionOrder.CurrencyPair.TickSize * settings.StopLossPriceDifferneceFactor, currentLowPeriodCandle.MaxPrice-currentLowPeriodCandle.MinPrice}.Max(),
							activeOrderPair.StopLossOrder.StopPrice ?? 0
						}.Min()
					};
			}
			return new PendingOrderInfo();
		}

		private async Task<decimal> GetNearestBidSupportPrice(CurrencyPair currencyPair)
		{
			var orderBookBidItems = (await _marketDataConnector.GetOrderBook(currencyPair.Id, 20))
				.Where(item => item.Type == OrderBookItemType.Bid)
				.ToList();

			if (!orderBookBidItems.Any())
				throw new NoNullAllowedException("Couldn't load order book");

			var avgBidSize = orderBookBidItems
				.Average(item => item.Size);

			var topBidPrice = orderBookBidItems
				.OrderByDescending(item => item.Price)
				.Select(item => item.Price)
				.First();

			return orderBookBidItems
				.Where(item => item.Size > avgBidSize && item.Price < topBidPrice)
				.OrderByDescending(item => item.Price)
				.Select(item => item.Price)
				.First();
		}
	}
}
