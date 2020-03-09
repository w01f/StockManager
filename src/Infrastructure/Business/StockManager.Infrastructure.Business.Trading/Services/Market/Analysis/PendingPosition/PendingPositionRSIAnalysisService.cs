using System.Data;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition
{
	public class PendingPositionRSIAnalysisService : IMarketPendingPositionAnalysisService
	{
		private readonly CandleLoadingService _candleLoadingService;
		private readonly ConfigurationService _configurationService;

		public PendingPositionRSIAnalysisService(CandleLoadingService candleLoadingService,
			ConfigurationService configurationService)
		{
			_candleLoadingService = candleLoadingService;
			_configurationService = configurationService;
		}

		public async Task<PendingPositionInfo> ProcessMarketPosition(TradingPosition activeTradingPosition)
		{
			var settings = _configurationService.GetTradingSettings();

			var targetPeriodLastCandles = (await _candleLoadingService.LoadCandles(
				activeTradingPosition.OpenPositionOrder.CurrencyPair.Id,
				settings.Period,
				2,
				settings.Moment))
				.ToList();

			if (!targetPeriodLastCandles.Any())
				throw new NoNullAllowedException("No candles loaded");

			var currentCandle = targetPeriodLastCandles.FirstOrDefault(candle => candle.Moment == settings.Moment);
			if (currentCandle != null && currentCandle.MaxPrice < activeTradingPosition.OpenPositionOrder.Price)
				return new CancelOrderInfo();

			//TODO: Try to implement checking on low period based on RSI - open order if RSI signals on low period too
			var lowerPeriodCandle = (await _candleLoadingService.LoadCandles(
					activeTradingPosition.OpenPositionOrder.CurrencyPair.Id,
					settings.Period.GetLowerFramePeriod(),
					1,
					settings.Moment))
				.FirstOrDefault();

			if (lowerPeriodCandle == null)
				throw new NoNullAllowedException("No candles loaded");

			return new PendingOrderInfo();
		}
	}
}
