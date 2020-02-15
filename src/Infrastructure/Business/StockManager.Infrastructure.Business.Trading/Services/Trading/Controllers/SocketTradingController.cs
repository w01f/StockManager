using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManager.Domain.Core.Entities.Trading;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Common;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Positions;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers
{
	class SocketTradingController : ITradingController
	{
		private readonly IRepository<Order> _orderRepository;
		private readonly IMarketDataSocketConnector _marketDataSocketConnector;
		private readonly IMarketDataRestConnector _marketDataRestConnector;
		private readonly ITradingDataRestConnector _tradingDataRestConnector;
		private readonly CandleLoadingService _candleLoadingService;
		private readonly IMarketNewPositionAnalysisService _marketNewPositionAnalysisService;
		private readonly ITradingPositionWorkerFactory _tradingPositionWorkerFactory;
		private readonly ConfigurationService _configurationService;
		private readonly TradingEventsObserver _tradingEventsObserver;
		private readonly ILoggingService _loggingService;

		private IList<TradingPositionWorker> _activePositionWorkers;

		public SocketTradingController(
			IRepository<Order> orderRepository,
			IMarketDataSocketConnector marketDataSocketConnector,
			IMarketDataRestConnector marketDataRestConnector,
			ITradingDataRestConnector tradingDataRestConnector,
			CandleLoadingService candleLoadingService,
			IMarketNewPositionAnalysisService marketNewPositionAnalysisService,
			ITradingPositionWorkerFactory tradingPositionWorkerFactory,
			ConfigurationService configurationService,
			TradingEventsObserver tradingEventsObserver,
			ILoggingService loggingService)
		{
			_orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
			_marketDataSocketConnector = marketDataSocketConnector ?? throw new ArgumentNullException(nameof(marketDataSocketConnector));
			_marketDataRestConnector = marketDataRestConnector ?? throw new ArgumentNullException(nameof(marketDataRestConnector));
			_tradingDataRestConnector = tradingDataRestConnector ?? throw new ArgumentNullException(nameof(tradingDataRestConnector));
			_candleLoadingService = candleLoadingService ?? throw new ArgumentNullException(nameof(candleLoadingService));
			_marketNewPositionAnalysisService = marketNewPositionAnalysisService ?? throw new ArgumentNullException(nameof(marketNewPositionAnalysisService));
			_tradingPositionWorkerFactory = tradingPositionWorkerFactory ?? throw new ArgumentNullException(nameof(tradingPositionWorkerFactory));
			_configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
			_tradingEventsObserver = tradingEventsObserver ?? throw new ArgumentNullException(nameof(loggingService));
			_loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
		}

		public async Task StartTrading(CancellationToken cancellationToken)
		{
			await SubscribeOnTradingEvents();
			await LoadExistingPositions();

			while (!cancellationToken.CanBeCanceled)
				await Task.Delay(1000, cancellationToken);
			//TODO Check if errors occured to exit
		}

		private async Task SubscribeOnTradingEvents()
		{
			var tradingSettings = _configurationService.GetTradingSettings();
			var allCurrencyPairs = await _marketDataRestConnector.GetCurrensyPairs();
			var tradingCurrencyPairs = allCurrencyPairs.Where(item => tradingSettings.QuoteCurrencies.Any(currencyId =>
					String.Equals(item.QuoteCurrencyId, currencyId, StringComparison.OrdinalIgnoreCase)))
				.ToList();

			foreach (var currencyPair in tradingCurrencyPairs)
			{
				var periodsForAnalysis = new[]
				{
					tradingSettings.Period.GetLowerFramePeriod(),
					tradingSettings.Period,
					tradingSettings.Period.GetHigherFramePeriod()
				};
				_candleLoadingService.InitSubscription(currencyPair.Id, periodsForAnalysis);

				await _marketDataSocketConnector.SubscribeOnTickers(currencyPair.Id, async ticker =>
				{
					if (_activePositionWorkers == null)
						return;

					if (_activePositionWorkers.Any(activePosition => activePosition.Position.CurrencyPairId == ticker.CurrencyPairId))
						return;

					await CheckOutForNewPosition(ticker);
				});
			}
		}

		private async Task CheckOutForNewPosition(Ticker ticker)
		{
			var tradingSettings = _configurationService.GetTradingSettings();
			var allCurrencyPairs = await _marketDataRestConnector.GetCurrensyPairs();
			var tickerCurrencyPair = allCurrencyPairs.Single(currencyPair => currencyPair.Id == ticker.CurrencyPairId);

			decimal volumeToCompare;
			if (String.Equals(tickerCurrencyPair.QuoteCurrencyId, Constants.BTC, StringComparison.OrdinalIgnoreCase))
				volumeToCompare = ticker.VolumeInQuoteCurrency;
			else
			{
				var btcConvertFactor = 0m;
				var requestCurrencyPair = allCurrencyPairs.FirstOrDefault(item =>
					String.Equals(tickerCurrencyPair.QuoteCurrencyId, item.QuoteCurrencyId, StringComparison.OrdinalIgnoreCase) &&
					String.Equals(item.BaseCurrencyId, Constants.BTC, StringComparison.OrdinalIgnoreCase));
				if (requestCurrencyPair != null)
				{
					var allTickers = await _marketDataRestConnector.GetTickers();
					btcConvertFactor = allTickers.First(item =>
						String.Equals(item.CurrencyPairId, requestCurrencyPair.Id, StringComparison.OrdinalIgnoreCase)).LastPrice;
				}
				else
				{
					requestCurrencyPair = allCurrencyPairs.FirstOrDefault(item =>
						String.Equals(tickerCurrencyPair.QuoteCurrencyId, item.BaseCurrencyId, StringComparison.OrdinalIgnoreCase) &&
						String.Equals(item.QuoteCurrencyId, Constants.BTC, StringComparison.OrdinalIgnoreCase));
					if (requestCurrencyPair != null)
					{
						var allTickers = await _marketDataRestConnector.GetTickers();
						btcConvertFactor = 1 / allTickers.First(item =>
												String.Equals(item.CurrencyPairId, requestCurrencyPair.Id,
													StringComparison.OrdinalIgnoreCase)).LastPrice;
					}
				}

				volumeToCompare = btcConvertFactor > 0 ? ticker.VolumeInQuoteCurrency / btcConvertFactor : 0;
			}

			if (volumeToCompare > tradingSettings.MinCurrencyPairTradingVolumeInBTC)
			{
				var tradingBalance = await _tradingDataRestConnector.GetTradingBallnce(tickerCurrencyPair.QuoteCurrencyId);
				if (tradingBalance?.Available > 0)
				{
					var marketInfo = await _marketNewPositionAnalysisService.ProcessMarketPosition(tickerCurrencyPair);
					if (marketInfo.PositionType != NewMarketPositionType.Wait)
					{
						var worker = await _tradingPositionWorkerFactory.CreateWorkerWithNewPosition((NewOrderPositionInfo)marketInfo, OnPositionChanged);
						_activePositionWorkers.Add(worker);
					}
				}
			}
		}

		private async Task LoadExistingPositions()
		{
			if (_activePositionWorkers?.Any() == true)
				throw new BusinessException("Positions already loaded");

			_activePositionWorkers = new List<TradingPositionWorker>();

			var storedOrderPairs = _orderRepository.GetAll().ToList().GenerateOrderPairs();
			foreach (var storedOrderTuple in storedOrderPairs)
			{
				var currencyPair = await _marketDataRestConnector.GetCurrensyPair(storedOrderTuple.Item1.CurrencyPair);

				if (currencyPair == null)
					throw new BusinessException("Currency pair not found")
					{
						Details = $"Currency pair id: {storedOrderTuple.Item1.CurrencyPair}"
					};

				var worker = _tradingPositionWorkerFactory.CreateWorkerWithExistingPosition(storedOrderTuple.ToModel(currencyPair), OnPositionChanged);
				_activePositionWorkers.Add(worker);
			}
		}

		private void OnPositionChanged(TradingPositionWorker worker, PositionChangedEventArgs eventArgs)
		{
			switch (eventArgs.EventType)
			{
				case TradingEventType.PositionClosedSuccessfully:
				case TradingEventType.PositionClosedDueStopLoss:
					_activePositionWorkers.Remove(worker);
					break;
			}

			_tradingEventsObserver.RaisePositionChanged(eventArgs.EventType, eventArgs.Details);
		}
	}
}
