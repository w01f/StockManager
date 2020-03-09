using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Business.Trading.Services.Extensions.Connectors;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition
{
	public class PendingPositionWilliamsRAnalysisService : IMarketPendingPositionAnalysisService
	{
		private readonly CandleLoadingService _candleLoadingService;
		private readonly OrderBookLoadingService _orderBookLoadingService;
		private readonly IIndicatorComputingService _indicatorComputingService;
		private readonly ConfigurationService _configurationService;

		public PendingPositionWilliamsRAnalysisService(CandleLoadingService candleLoadingService,
			OrderBookLoadingService orderBookLoadingService,
			IIndicatorComputingService indicatorComputingService,
			ConfigurationService configurationService)
		{
			_candleLoadingService = candleLoadingService ?? throw new ArgumentNullException(nameof(candleLoadingService));
			_orderBookLoadingService = orderBookLoadingService ?? throw new ArgumentNullException(nameof(orderBookLoadingService));
			_indicatorComputingService = indicatorComputingService ?? throw new ArgumentNullException(nameof(indicatorComputingService));
			_configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
		}

		public async Task<PendingPositionInfo> ProcessMarketPosition(TradingPosition activeTradingPosition)
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
				activeTradingPosition.OpenPositionOrder.CurrencyPair.Id,
				settings.Period,
				williamsRSettings.Period * 2,
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
				((activeTradingPosition.OpenPositionOrder.OrderStateType == OrderStateType.Suspended && currentWilliamsRValue.Value < previousWilliamsRValue?.Value) ||
				(activeTradingPosition.OpenPositionOrder.OrderStateType != OrderStateType.Suspended)) &&
				Math.Abs(maxWilliamsRValue - currentWilliamsRValue.Value ?? 0) < 20)
			{
				decimal stopOpenPrice;
				decimal openPrice;
				if (activeTradingPosition.OpenPositionOrder.OrderStateType == OrderStateType.Suspended)
				{
					var topBidPrice = await _orderBookLoadingService.GetTopBidPrice(activeTradingPosition.OpenPositionOrder.CurrencyPair);

					openPrice = activeTradingPosition.OpenPositionOrder.Price == topBidPrice ?
						topBidPrice :
						(topBidPrice + activeTradingPosition.OpenPositionOrder.CurrencyPair.TickSize);

					var bottomMeaningfulAskPrice = await _orderBookLoadingService.GetBottomAskPrice(activeTradingPosition.OpenPositionOrder.CurrencyPair, 1);

					stopOpenPrice = new[] { activeTradingPosition.OpenPositionOrder.StopPrice ?? 0, bottomMeaningfulAskPrice }.Min();
				}
				else
				{
					stopOpenPrice = 0;

					var topBidPrice = await _orderBookLoadingService.GetTopBidPrice(activeTradingPosition.OpenPositionOrder.CurrencyPair);

					openPrice = new[]
						{
							activeTradingPosition.OpenPositionOrder.Price == topBidPrice ? topBidPrice : (topBidPrice + activeTradingPosition.OpenPositionOrder.CurrencyPair.TickSize),
							activeTradingPosition.OpenPositionOrder.Price
						}.Max();
				}

				if (activeTradingPosition.OpenPositionOrder.Price != openPrice ||
					activeTradingPosition.OpenPositionOrder.StopPrice != stopOpenPrice)
					return new UpdateOrderInfo
					{
						OpenPrice = openPrice,
						OpenStopPrice = stopOpenPrice,

						ClosePrice = new[] { currentTargetPeriodCandle.MaxPrice, activeTradingPosition.ClosePositionOrder.Price }.Max(),
						CloseStopPrice = new[] { currentTargetPeriodCandle.MinPrice, activeTradingPosition.ClosePositionOrder.StopPrice ?? 0 }.Min(),

						StopLossPrice = new[]
						{
							currentTargetPeriodCandle.MinPrice - targetPeriodLastCandles.Select(candle=>(candle.MaxPrice-candle.MinPrice)*5).Average(),
							activeTradingPosition.StopLossOrder.StopPrice ?? 0
						}.Min()
					};

				return new PendingOrderInfo();
			}
			return new CancelOrderInfo();
		}
	}
}
