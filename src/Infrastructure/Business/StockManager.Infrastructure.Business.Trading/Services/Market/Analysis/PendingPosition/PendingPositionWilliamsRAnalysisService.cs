using System.Data;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Domain.Core.Entities.Market;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition
{
	public class PendingPositionWilliamsRAnalysisService : IMarketPendingPositionAnalysisService
	{
		private readonly IRepository<Candle> _candleRepository;
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly IIndicatorComputingService _indicatorComputingService;

		public PendingPositionWilliamsRAnalysisService(IRepository<Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService)
		{
			_candleRepository = candleRepository;
			_marketDataConnector = marketDataConnector;
			_indicatorComputingService = indicatorComputingService;
		}

		public async Task<PendingPositionInfo> ProcessMarketPosition(TradingSettings settings, OrderPair activeOrderPair)
		{
			var williamsRSettings = new CommonIndicatorSettings
			{
				Period = 5
			};

			var targetPeriodLastCandles = (await CandleLoader.Load(
				settings.CurrencyPairId,
				settings.Period,
				williamsRSettings.Period + 1,
				settings.Moment,
				_candleRepository,
				_marketDataConnector))
				.ToList();

			if (!targetPeriodLastCandles.Any())
				throw new NoNullAllowedException("No candles loaded");

			var currentCandle = targetPeriodLastCandles.FirstOrDefault(candle => candle.Moment == settings.Moment);

			if (currentCandle != null)
			{
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

				if (currentWilliamsRValue.Value >= 90)
				{
					decimal stopOpenPrice;
					decimal openPrice;
					if (activeOrderPair.OpenPositionOrder.OrderStateType == OrderStateType.Suspended)
					{
						stopOpenPrice = new[]
						{
							currentCandle.MaxPrice,
							activeOrderPair.OpenPositionOrder.StopPrice ?? 0
						}.Min();
						openPrice = stopOpenPrice - activeOrderPair.OpenPositionOrder.CurrencyPair.TickSize * settings.StopLimitPriceDifferneceFactor;
					}
					else
					{
						stopOpenPrice = 0;
						openPrice = new[]
						{
							currentCandle.MaxPrice,
							activeOrderPair.OpenPositionOrder.Price
						}.Max();
					}

					return new UpdateOrderInfo
					{
						OpenPrice = openPrice,
						OpenStopPrice = stopOpenPrice,

						ClosePrice = new[] { currentCandle.MaxPrice, activeOrderPair.ClosePositionOrder.Price }.Max(),
						CloseStopPrice = new[] { currentCandle.MinPrice, activeOrderPair.ClosePositionOrder.StopPrice ?? 0 }.Min(),

						StopLossPrice = new[] { currentCandle.MinPrice, activeOrderPair.StopLossOrder.Price }.Min()
					};
				}
				return new CancelOrderInfo();
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

				if (currentWilliamsRValue.Value >= 90)
				{
					decimal stopOpenPrice;
					decimal openPrice;
					if (activeOrderPair.OpenPositionOrder.OrderStateType == OrderStateType.Suspended)
					{
						stopOpenPrice = new[]
						{
							currentLowPeriodCandle.MaxPrice,
							activeOrderPair.OpenPositionOrder.StopPrice ?? 0
						}.Min();
						openPrice = stopOpenPrice - activeOrderPair.OpenPositionOrder.CurrencyPair.TickSize * settings.StopLimitPriceDifferneceFactor;
					}
					else
					{
						stopOpenPrice = 0;
						openPrice = new[]
						{
							currentLowPeriodCandle.MaxPrice,
							activeOrderPair.OpenPositionOrder.Price
						}.Max();
					}

					return new UpdateOrderInfo
					{
						OpenPrice = openPrice,
						OpenStopPrice = stopOpenPrice,

						ClosePrice = new[] { currentLowPeriodCandle.MaxPrice, activeOrderPair.ClosePositionOrder.Price }.Max(),
						CloseStopPrice = new[] { currentLowPeriodCandle.MinPrice, activeOrderPair.ClosePositionOrder.StopPrice ?? 0 }.Min(),

						StopLossPrice = new[] { currentLowPeriodCandle.MinPrice, activeOrderPair.StopLossOrder.Price }.Min()
					};
				}
				return new PendingOrderInfo();
			}
		}
	}
}
