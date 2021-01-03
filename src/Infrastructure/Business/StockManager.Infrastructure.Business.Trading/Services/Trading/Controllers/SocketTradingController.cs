using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManager.Domain.Core.Entities.Trading;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Common;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.AsyncWorker;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Utilities.Logging.Models.Errors;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers
{
	public class SocketTradingController : ITradingController
	{
		private readonly IRepository<Order> _orderRepository;
		private readonly IStockSocketConnector _stockSocketConnector;
		private readonly IStockRestConnector _stockRestConnector;
		private readonly CandleLoadingService _candleLoadingService;
		private readonly IMarketNewPositionAnalysisService _marketNewPositionAnalysisService;
		private readonly TradingPositionWorkerFactory _tradingPositionWorkerFactory;
		private readonly ITradingPositionService _tradingPositionService;
		private readonly ConfigurationService _configurationService;
		private readonly TradingEventsObserver _tradingEventsObserver;
		private readonly ILoggingService _loggingService;

		private ConcurrentDictionary<string, TradingPositionWorker> _activePositionWorkers;

		public SocketTradingController(
			IRepository<Order> orderRepository,
			IStockSocketConnector stockSocketConnector,
			IStockRestConnector stockRestConnector,
			CandleLoadingService candleLoadingService,
			IMarketNewPositionAnalysisService marketNewPositionAnalysisService,
			TradingPositionWorkerFactory tradingPositionWorkerFactory,
			ITradingPositionService tradingPositionService,
			ConfigurationService configurationService,
			TradingEventsObserver tradingEventsObserver,
			ILoggingService loggingService)
		{
			_orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
			_stockSocketConnector = stockSocketConnector ?? throw new ArgumentNullException(nameof(stockSocketConnector));
			_stockRestConnector = stockRestConnector ?? throw new ArgumentNullException(nameof(stockRestConnector));
			_candleLoadingService = candleLoadingService ?? throw new ArgumentNullException(nameof(candleLoadingService));
			_marketNewPositionAnalysisService = marketNewPositionAnalysisService ?? throw new ArgumentNullException(nameof(marketNewPositionAnalysisService));
			_tradingPositionWorkerFactory = tradingPositionWorkerFactory ?? throw new ArgumentNullException(nameof(tradingPositionWorkerFactory));
			_tradingPositionService = tradingPositionService ?? throw new ArgumentNullException(nameof(tradingPositionService));
			_configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
			_tradingEventsObserver = tradingEventsObserver ?? throw new ArgumentNullException(nameof(tradingEventsObserver));
			_loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
		}

		public event EventHandler<UnhandledExceptionEventArgs> Exception;

		public void StartTrading()
		{
			var cancelTokenSource = new CancellationTokenSource();
			try
			{
				var tradingSettings = _configurationService.GetTradingSettings();
				tradingSettings.Period = CandlePeriod.Minute5;
				tradingSettings.Moment = null;
				tradingSettings.BaseOrderSide = OrderSide.Buy;
				_configurationService.UpdateTradingSettings(tradingSettings);

				_stockSocketConnector.Connect().Wait(cancelTokenSource.Token);
				_stockSocketConnector.SubscribeErrors(exception =>
				{
					OnException(new UnhandledExceptionEventArgs(exception, false));
					cancelTokenSource.Cancel();
				});

				Task.WaitAll(StartTradingInner(cancelTokenSource.Token));
			}
			catch (BusinessWarning e)
			{
				_loggingService.LogAction(new ErrorAction
				{
					ExceptionType = e.GetType().ToString(),
					Message = e.Message,
					Details = e.Details,
					StackTrace = e.StackTrace
				});
			}
			catch (BusinessException e)
			{
				_loggingService.LogAction(new ErrorAction
				{
					ExceptionType = e.GetType().ToString(),
					Message = e.Message,
					Details = e.Details,
					StackTrace = e.StackTrace
				});
				OnException(new UnhandledExceptionEventArgs(e, false));
			}
			catch (ParseResponseException e)
			{
				_loggingService.LogAction(new ErrorAction
				{
					ExceptionType = e.GetType().ToString(),
					Message = e.Message,
					Details = e.SourceData,
					StackTrace = e.StackTrace
				});
				OnException(new UnhandledExceptionEventArgs(e, false));
			}
			catch (Exception e)
			{
				_loggingService.LogAction(new ErrorAction
				{
					ExceptionType = e.GetType().ToString(),
					Message = e.Message,
					StackTrace = e.StackTrace
				});
				OnException(new UnhandledExceptionEventArgs(e, false));
			}
			finally
			{
				cancelTokenSource.Cancel();
			}
		}

		private async Task StartTradingInner(CancellationToken cancellationToken)
		{
			await _stockSocketConnector.Connect();

			await _tradingPositionService.SyncExistingPositionsWithStock(positionChangedEventArgs => _tradingEventsObserver.RaisePositionChanged(positionChangedEventArgs));
			await LoadExistingPositions();

			await CheckOutNewTickersForRequiredVolume();

			WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle });

			await _stockSocketConnector.Disconnect();
		}

		private async Task CheckOutNewTickersForRequiredVolume()
		{
			var tradingSettings = _configurationService.GetTradingSettings();
			var allCurrencyPairs = await _stockRestConnector.GetCurrencyPairs();

			var baseCurrencyPairs = allCurrencyPairs.Where(item => !_activePositionWorkers.ContainsKey(item.Id) &&
				tradingSettings.QuoteCurrencies.Any(currencyId => String.Equals(item.QuoteCurrencyId, currencyId, StringComparison.OrdinalIgnoreCase)));

			var allTickers = await _stockRestConnector.GetTickers();
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

			var tradingCurrencyPairs = baseCurrencyPairs.Where(currencyPairItem => tradingTickers.Any(tickerItem =>
				String.Equals(tickerItem.CurrencyPairId, currencyPairItem.Id, StringComparison.OrdinalIgnoreCase))).ToList();

			var periodsForAnalysis = new[]
			{
				tradingSettings.Period.GetLowerFramePeriod(),
				tradingSettings.Period,
				tradingSettings.Period.GetHigherFramePeriod()
			};

			foreach (var currencyPair in tradingCurrencyPairs)
			{
				foreach (var candlePeriod in periodsForAnalysis)
				{
					var candles = await _stockRestConnector.GetCandles(currencyPair.Id, candlePeriod, 30);
					_candleLoadingService.UpdateCandles(currencyPair.Id, candlePeriod, candles);
				}

				await _candleLoadingService.InitSubscription(currencyPair.Id, periodsForAnalysis);

				_candleLoadingService.CandlesUpdated += (o, e) =>
				{
					if (_activePositionWorkers == null)
						return;

					if (_activePositionWorkers.ContainsKey(e.CurrencyPairId))
						return;

					if (e.Period != tradingSettings.Period)
						return;

					CheckOutForNewPosition(currencyPair).Wait();
				};
			}
		}

		private async Task CheckOutForNewPosition(CurrencyPair tickerCurrencyPair)
		{
			var marketInfo = await _marketNewPositionAnalysisService.ProcessMarketPosition(tickerCurrencyPair);
			if (marketInfo.PositionType != NewMarketPositionType.Wait && !_activePositionWorkers.ContainsKey(tickerCurrencyPair.Id))
			{
				var worker = _tradingPositionWorkerFactory.CreateWorkerWithNewPosition(OnPositionChanged);
				var success = _activePositionWorkers.TryAdd(tickerCurrencyPair.Id, worker);
				if (success)
					await worker.CreateNewPosition((NewOrderPositionInfo)marketInfo);
			}
		}

		private async Task LoadExistingPositions()
		{
			if (_activePositionWorkers?.Any() == true)
				throw new BusinessException("Positions already loaded");

			_activePositionWorkers = new ConcurrentDictionary<string, TradingPositionWorker>();

			var storedOrderPairs = _orderRepository.GetAll().ToList().GroupOrders();
			foreach (var storedOrderTuple in storedOrderPairs)
			{
				var currencyPair = await _stockRestConnector.GetCurrencyPair(storedOrderTuple.Item1.CurrencyPair);

				if (currencyPair == null)
					throw new BusinessException("Currency pair not found")
					{
						Details = $"Currency pair id: {storedOrderTuple.Item1.CurrencyPair}"
					};

				var worker = await _tradingPositionWorkerFactory.CreateWorkerWithExistingPosition(storedOrderTuple.ToTradingPosition(currencyPair), OnPositionChanged);
				_activePositionWorkers.TryAdd(currencyPair.Id, worker);
			}
		}

		private void OnPositionChanged(TradingPositionWorker worker, PositionChangedEventArgs eventArgs)
		{
			if (eventArgs.Position.IsClosedPosition)
				_activePositionWorkers.TryRemove(worker.Position.CurrencyPairId, out _);

			_tradingEventsObserver.RaisePositionChanged(eventArgs.EventType, eventArgs.Position);
		}

		private void OnException(UnhandledExceptionEventArgs e)
		{
			Exception?.Invoke(this, e);
		}
	}
}
