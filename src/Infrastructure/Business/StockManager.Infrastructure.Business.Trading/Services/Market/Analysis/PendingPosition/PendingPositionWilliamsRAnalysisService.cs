using System;
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
		private readonly IMarketDataRestConnector _marketDataRestConnector;
		private readonly IIndicatorComputingService _indicatorComputingService;
		private readonly ConfigurationService _configurationService;

		public PendingPositionWilliamsRAnalysisService(CandleLoadingService candleLoadingService,
			IMarketDataRestConnector marketDataRestConnector,
			IIndicatorComputingService indicatorComputingService,
			ConfigurationService configurationService)
		{
			_candleLoadingService = candleLoadingService;
			_marketDataRestConnector = marketDataRestConnector;
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

			var higherPeriodMACDSettings = new MACDSettings
			{
				EMAPeriod1 = 12,
				EMAPeriod2 = 26,
				SignalPeriod = 9
			};

			var targetPeriodLastCandles = (await _candleLoadingService.LoadCandles(
				activeOrderPair.OpenPositionOrder.CurrencyPair.Id,
				settings.Period,
				williamsRSettings.Period * 2,
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
					williamsRSettings.Period,
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

					currentTargetPeriodCandle = targetPeriodLastCandles.Last();
				}
			}

			var williamsRValues = _indicatorComputingService.ComputeWilliamsR(
					targetPeriodLastCandles,
					williamsRSettings.Period)
				.OfType<SimpleIndicatorValue>()
				.ToList();
			var currentWilliamsRValue = williamsRValues.ElementAtOrDefault(williamsRValues.Count - 1);
			var previousWilliamsRValue = williamsRValues.ElementAtOrDefault(williamsRValues.Count - 2);
			var maxWilliamsRValue = williamsRValues.Where(item => item.Value.HasValue).Select(item => item.Value.Value).Max();

			if (currentWilliamsRValue?.Value == null)
				throw new NoNullAllowedException("No WilliamR values calculated");

			var higherPeriodMACDValues = _indicatorComputingService.ComputeMACD(
					higherPeriodLastCandles,
					higherPeriodMACDSettings.EMAPeriod1,
					higherPeriodMACDSettings.EMAPeriod2,
					higherPeriodMACDSettings.SignalPeriod)
				.OfType<MACDValue>()
				.ToList();

			var higherPeriodCurrentMACDValue = higherPeriodMACDValues.ElementAtOrDefault(higherPeriodMACDValues.Count - 1);
			var useExtendedBorders = higherPeriodCurrentMACDValue?.Histogram >= 0;

			if (currentWilliamsRValue.Value >= (useExtendedBorders ? 50 : 80) &&
				currentWilliamsRValue.Value < 95 &&
				((activeOrderPair.OpenPositionOrder.OrderStateType == OrderStateType.Suspended && currentWilliamsRValue.Value < previousWilliamsRValue?.Value) ||
				(activeOrderPair.OpenPositionOrder.OrderStateType != OrderStateType.Suspended)) &&
				Math.Abs(maxWilliamsRValue - currentWilliamsRValue.Value ?? 0) < 20)
			{
				decimal stopOpenPrice;
				decimal openPrice;
				if (activeOrderPair.OpenPositionOrder.OrderStateType == OrderStateType.Suspended)
				{
					var orderBookBidItems = (await _marketDataRestConnector.GetOrderBook(activeOrderPair.OpenPositionOrder.CurrencyPair.Id, 5))
						.Where(item => item.Type == OrderBookItemType.Bid)
						.ToList();

					if (!orderBookBidItems.Any())
						throw new NoNullAllowedException("Couldn't load order book");

					var topBidPrice = orderBookBidItems
						.OrderByDescending(item => item.Price)
						.Select(item => item.Price)
						.First();

					openPrice = activeOrderPair.OpenPositionOrder.Price == topBidPrice ?
						topBidPrice :
						(topBidPrice + activeOrderPair.OpenPositionOrder.CurrencyPair.TickSize);

					var orderBookAskItems = (await _marketDataRestConnector.GetOrderBook(activeOrderPair.OpenPositionOrder.CurrencyPair.Id, 5))
						.Where(item => item.Type == OrderBookItemType.Ask)
						.ToList();

					if (!orderBookAskItems.Any())
						throw new NoNullAllowedException("Couldn't load order book");

					var bottomMeaningfulAskPrice = orderBookAskItems
						.OrderBy(item => item.Price)
						.Skip(1)
						.Select(item => item.Price)
						.First();

					stopOpenPrice = new[] { activeOrderPair.OpenPositionOrder.StopPrice ?? 0, bottomMeaningfulAskPrice }.Min();
				}
				else
				{
					stopOpenPrice = 0;

					var orderBookBidItems = (await _marketDataRestConnector.GetOrderBook(activeOrderPair.OpenPositionOrder.CurrencyPair.Id, 20))
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
							activeOrderPair.OpenPositionOrder.Price==topBidPrice?
								topBidPrice:
								(topBidPrice+activeOrderPair.OpenPositionOrder.CurrencyPair.TickSize),
							activeOrderPair.OpenPositionOrder.Price
						}.Max();
				}

				if (activeOrderPair.OpenPositionOrder.Price != openPrice ||
					activeOrderPair.OpenPositionOrder.StopPrice != stopOpenPrice)
					return new UpdateOrderInfo
					{
						OpenPrice = openPrice,
						OpenStopPrice = stopOpenPrice,

						ClosePrice = new[] { currentTargetPeriodCandle.MaxPrice, activeOrderPair.ClosePositionOrder.Price }.Max(),
						CloseStopPrice = new[] { currentTargetPeriodCandle.MinPrice, activeOrderPair.ClosePositionOrder.StopPrice ?? 0 }.Min(),

						StopLossPrice = new[]
						{
							currentTargetPeriodCandle.MinPrice - targetPeriodLastCandles.Select(candle=>(candle.MaxPrice-candle.MinPrice)*5).Average(),
							activeOrderPair.StopLossOrder.StopPrice ?? 0
						}.Min()
					};

				return new PendingOrderInfo();
			}
			return new CancelOrderInfo();
		}
	}
}
