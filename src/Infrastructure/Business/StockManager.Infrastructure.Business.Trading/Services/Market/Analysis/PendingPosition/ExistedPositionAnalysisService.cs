using System.Data;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Entities.Market;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition
{
	public class PendingPositionAnalysisService : IMarketPendingPositionAnalysisService
	{
		private readonly IRepository<Candle> _candleRepository;
		private readonly IMarketDataConnector _marketDataConnector;

		public PendingPositionAnalysisService(IRepository<Candle> candleRepository,
			IMarketDataConnector marketDataConnector)
		{
			_candleRepository = candleRepository;
			_marketDataConnector = marketDataConnector;
		}

		public async Task<PendingPositionInfo> ProcessMarketPosition(TradingSettings settings, OrderPair activeOrderPair)
		{
			var targetPeriodLastCandles = (await CandleLoader.Load(
				settings.CurrencyPairId,
				settings.Period,
				2,
				settings.Moment,
				_candleRepository,
				_marketDataConnector))
				.ToList();

			if (!targetPeriodLastCandles.Any())
				throw new NoNullAllowedException("No candles loaded");

			var currentCandle = targetPeriodLastCandles.FirstOrDefault(candle => candle.Moment == settings.Moment);
			if (currentCandle != null && currentCandle.MaxPrice < activeOrderPair.InitialOrder.Price)
				return new CancelOrderInfo();

			//TODO: Try to implement checking on low period based on RSI - open order if RSI signals on low period too
			var lowerPeriodCandle = (await CandleLoader.Load(
					settings.CurrencyPairId,
					settings.Period.GetLowerFramePeriod(),
					1,
					settings.Moment,
					_candleRepository,
					_marketDataConnector))
				.FirstOrDefault();

			if (lowerPeriodCandle == null)
				throw new NoNullAllowedException("No candles loaded");

			return new PendingOrderInfo();
		}
	}
}
