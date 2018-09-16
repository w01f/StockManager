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
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition
{
	public class PendingPositionRSIAnalysisService : IMarketPendingPositionAnalysisService
	{
		private readonly IRepository<Candle> _candleRepository;
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly ConfigurationService _configurationService;

		public PendingPositionRSIAnalysisService(IRepository<Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			ConfigurationService configurationService)
		{
			_candleRepository = candleRepository;
			_marketDataConnector = marketDataConnector;
			_configurationService = configurationService;
		}

		public async Task<PendingPositionInfo> ProcessMarketPosition(OrderPair activeOrderPair)
		{
			var settings = _configurationService.GetTradingSettings();

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
			if (currentCandle != null && currentCandle.MaxPrice < activeOrderPair.OpenPositionOrder.Price)
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
