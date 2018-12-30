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
				Period = 10
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

				var currentWilliamsRValue = williamsRValues.ElementAtOrDefault(williamsRValues.Count - 1);

				if (currentWilliamsRValue?.Value == null)
					throw new NoNullAllowedException("No WilliamR values calculated");

				if (currentWilliamsRValue.Value >= 90)
				{
					decimal stopOpenPrice;
					decimal openPrice;
					if (activeOrderPair.OpenPositionOrder.OrderStateType == OrderStateType.Suspended)
					{
						var orderBookBidItems = (await _marketDataConnector.GetOrderBook(activeOrderPair.OpenPositionOrder.CurrencyPair.Id, 5))
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

						openPrice = topMeaningfulBidPrice;
					
						var orderBookAskItems = (await _marketDataConnector.GetOrderBook(activeOrderPair.OpenPositionOrder.CurrencyPair.Id, 5))
							.Where(item => item.Type == OrderBookItemType.Ask)
							.ToList();

						if (!orderBookAskItems.Any())
							throw new NoNullAllowedException("Couldn't load order book");

						var bottomMeaningfulAskPrice = orderBookAskItems
							.OrderBy(item => item.Price)
							.Skip(1)
							.Select(item => item.Price)
							.First();
						
						stopOpenPrice = bottomMeaningfulAskPrice;
					}
					else
					{
						stopOpenPrice = 0;

						var orderBookBidItems = (await _marketDataConnector.GetOrderBook(activeOrderPair.OpenPositionOrder.CurrencyPair.Id, 20))
							.Where(item => item.Type == OrderBookItemType.Bid)
							.ToList();

						if (!orderBookBidItems.Any())
							throw new NoNullAllowedException("Couldn't load order book");

						var topBidPrice = orderBookBidItems
							.OrderByDescending(item => item.Price)
							.Select(item => item.Price)
							.First();

						openPrice = new[]
							{
								topBidPrice,
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
			
			if (activeOrderPair.OpenPositionOrder.OrderStateType != OrderStateType.Suspended)
			{
				var williamsRValues = _indicatorComputingService.ComputeWilliamsR(
						lowerPeriodCandles,
						williamsRSettings.Period)
					.OfType<SimpleIndicatorValue>()
					.ToList();

				var currentWilliamsRValue = williamsRValues.ElementAtOrDefault(williamsRValues.Count - 1);
				if (currentWilliamsRValue != null && currentWilliamsRValue.Value < 50)
				{
					var orderBookBidItems = (await _marketDataConnector.GetOrderBook(activeOrderPair.OpenPositionOrder.CurrencyPair.Id, 20))
						.Where(item => item.Type == OrderBookItemType.Bid)
						.ToList();

					if (!orderBookBidItems.Any())
						throw new NoNullAllowedException("Couldn't load order book");

					var topBidPrice = orderBookBidItems
						.OrderByDescending(item => item.Price)
						.Select(item => item.Price)
						.First();

					var openPrice = new[]
					{
						topBidPrice,
						activeOrderPair.OpenPositionOrder.Price
					}.Max();

					if (activeOrderPair.OpenPositionOrder.Price != openPrice)
						return new UpdateOrderInfo
						{
							OpenPrice = openPrice,
							OpenStopPrice = 0,

							ClosePrice = new[] { currentLowPeriodCandle.MaxPrice, activeOrderPair.ClosePositionOrder.Price }.Max(),
							CloseStopPrice = new[] { currentLowPeriodCandle.MinPrice, activeOrderPair.ClosePositionOrder.StopPrice ?? 0 }.Min(),

							StopLossPrice = new[]
							{
								currentLowPeriodCandle.MinPrice - new[]{activeOrderPair.OpenPositionOrder.CurrencyPair.TickSize * settings.StopLossPriceDifferneceFactor, currentLowPeriodCandle.MaxPrice-currentLowPeriodCandle.MinPrice}.Max(),
								activeOrderPair.StopLossOrder.StopPrice ?? 0
							}.Min()
						};
				}

			}
			return new PendingOrderInfo();
		}
	}
}
