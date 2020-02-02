using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers
{
	class SocketTradingController : ITradingController
	{
		private readonly IMarketDataSocketConnector _marketDataSocketConnector;
		private readonly IMarketDataRestConnector _marketDataRestConnector;
		private readonly CandleLoadingService _candleLoadingService;
		private readonly IMarketNewPositionAnalysisService _marketNewPositionAnalysisService;
		private readonly IMarketPendingPositionAnalysisService _marketPendingPositionAnalysisService;
		private readonly IMarketOpenPositionAnalysisService _marketOpenPositionAnalysisService;
		private readonly ConfigurationService _configurationService;
		private readonly ILoggingService _loggingService;

		private readonly IList<OrderPair> _activeOrderPairs = new List<OrderPair>();

		public SocketTradingController(IMarketDataSocketConnector marketDataSocketConnector,
			IMarketDataRestConnector marketDataRestConnector,
			CandleLoadingService candleLoadingService,
			IMarketNewPositionAnalysisService marketNewPositionAnalysisService,
			IMarketPendingPositionAnalysisService marketPendingPositionAnalysisService,
			IMarketOpenPositionAnalysisService marketOpenPositionAnalysisService,
			ConfigurationService configurationService,
			ILoggingService loggingService)
		{
			_marketDataSocketConnector = marketDataSocketConnector ?? throw new ArgumentNullException(nameof(marketDataSocketConnector));
			_marketDataRestConnector = marketDataRestConnector ?? throw new ArgumentNullException(nameof(marketDataRestConnector));
			_candleLoadingService = candleLoadingService ?? throw new ArgumentNullException(nameof(marketDataRestConnector));
			_marketNewPositionAnalysisService = marketNewPositionAnalysisService ?? throw new ArgumentNullException(nameof(marketNewPositionAnalysisService));
			_marketPendingPositionAnalysisService = marketPendingPositionAnalysisService ?? throw new ArgumentNullException(nameof(marketPendingPositionAnalysisService));
			_marketOpenPositionAnalysisService = marketOpenPositionAnalysisService ?? throw new ArgumentNullException(nameof(marketOpenPositionAnalysisService));
			_configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
			_loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
		}

		public async Task StartTrading()
		{
			LoadExistingPositions();

			var allTickers = await _marketDataRestConnector.GetTickers();
			await AnalyzeCurrencyPairsForNewPosition(allTickers);

			_marketDataSocketConnector.SubscribeOnTickers(async tickers =>
			{
				await AnalyzeCurrencyPairsForNewPosition(tickers);
			});

			//_marketDataSocketConnector.SubscribeOnOrderBook(async orderBookItems =>
			//{
			//	foreach (var orderPair in _activeOrderPairs)
			//	{
			//		if()
			//	}
			//});

			//TODO Add endless cycle here
			//TODO Check if errors occured to exit
		}

		private void AnalyzeNewPosition(CurrencyPair currencyPair)
		{

		}

		private void LoadExistingPositions()
		{

		}

		private async Task AnalyzeCurrencyPairsForNewPosition(IList<Ticker> allTickers)
		{
			var tradingSettings = _configurationService.GetTradingSettings();

			var allCurrencyPairs = await _marketDataRestConnector.GetCurrensyPairs();

			var baseCurrencyPairs = allCurrencyPairs
				.Where(item => tradingSettings.QuoteCurrencies.Any(currencyId =>
									String.Equals(item.QuoteCurrencyId, currencyId, StringComparison.OrdinalIgnoreCase)) &&
								!_activeOrderPairs.Any(orderPair => String.Equals(orderPair.OpenPositionOrder.CurrencyPair.BaseCurrencyId, item.BaseCurrencyId))
				);

			var tradingTickers = allTickers
						.Where(tickerItem =>
							baseCurrencyPairs.Any(currencyPairItem => String.Equals(tickerItem.CurrencyPairId, currencyPairItem.Id)))
						.Where(tickerItem =>
						{
							var tickerCurrencyPair = baseCurrencyPairs.First(currencyPairItem =>
								String.Equals(tickerItem.CurrencyPairId, currencyPairItem.Id));
							decimal volumeToCompare;
							if (String.Equals(tickerCurrencyPair.QuoteCurrencyId, Constants.BTC, StringComparison.OrdinalIgnoreCase))
								volumeToCompare = tickerItem.VolumeInQuoteCurrency;
							else
							{
								var btcConvertFactor = 0m;
								var requestCurrencyPair = allCurrencyPairs.FirstOrDefault(item =>
									String.Equals(tickerCurrencyPair.QuoteCurrencyId, item.QuoteCurrencyId, StringComparison.OrdinalIgnoreCase) &&
									String.Equals(item.BaseCurrencyId, Constants.BTC, StringComparison.OrdinalIgnoreCase));
								if (requestCurrencyPair != null)
									btcConvertFactor = allTickers.First(item =>
										String.Equals(item.CurrencyPairId, requestCurrencyPair.Id, StringComparison.OrdinalIgnoreCase)).LastPrice;
								else
								{
									requestCurrencyPair = allCurrencyPairs.FirstOrDefault(item =>
										String.Equals(tickerCurrencyPair.QuoteCurrencyId, item.BaseCurrencyId, StringComparison.OrdinalIgnoreCase) &&
										String.Equals(item.QuoteCurrencyId, Constants.BTC, StringComparison.OrdinalIgnoreCase));
									if (requestCurrencyPair != null)
										btcConvertFactor = 1 / allTickers.First(item =>
															   String.Equals(item.CurrencyPairId, requestCurrencyPair.Id,
																   StringComparison.OrdinalIgnoreCase)).LastPrice;
								}

								volumeToCompare = btcConvertFactor > 0 ? tickerItem.VolumeInQuoteCurrency / btcConvertFactor : 0;
							}
							return volumeToCompare > tradingSettings.MinCurrencyPairTradingVolumeInBTC;
						})
						.ToList();

			//foreach (var currencyPair in baseCurrencyPairs.Where(currencyPairItem => tradingTickers.Any(tickerItem =>
			//	String.Equals(tickerItem.CurrencyPairId, currencyPairItem.Id, StringComparison.OrdinalIgnoreCase))).ToList())
			//{
			//	var tradingBalance = await _tradingDataRestConnector.GetTradingBallnce(currencyPair.QuoteCurrencyId);
			//	if (tradingBalance?.Available <= 0)
			//		continue;

			//	var marketInfo = await _marketNewPositionAnalysisService.ProcessMarketPosition(currencyPair);
			//	if (marketInfo.PositionType != NewMarketPositionType.Wait)
			//	{
			//		await _orderService.OpenPosition((NewOrderPositionInfo)marketInfo);
			//		break;
			//	}
			//}
		}
	}
}
