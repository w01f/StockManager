using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Utilities.Logging.Models.Errors;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers
{
	public class TinkoffTradingAdvisorController
	{
		private readonly IStockSocketConnector _stockSocketConnector;
		private readonly CandleLoadingService _candleLoadingService;
		private readonly IMarketNewPositionAnalysisService _marketNewPositionAnalysisService;
		private readonly ConfigurationService _configurationService;
		private readonly ILoggingService _loggingService;

		private readonly ConcurrentDictionary<string, CurrencyPair> _suggestedCurrencyPairs = new ConcurrentDictionary<string, CurrencyPair>();

		public event EventHandler<CurrencyPairsEventArgs> SuggestedCurrencyPairsUpdated;
		public event EventHandler<UnhandledExceptionEventArgs> Exception;

		public TinkoffTradingAdvisorController(
			IStockSocketConnector stockSocketConnector,
			CandleLoadingService candleLoadingService,
			IMarketNewPositionAnalysisService marketNewPositionAnalysisService,
			ConfigurationService configurationService,
			ILoggingService loggingService)
		{
			_stockSocketConnector = stockSocketConnector ?? throw new ArgumentNullException(nameof(stockSocketConnector));
			_candleLoadingService = candleLoadingService ?? throw new ArgumentNullException(nameof(candleLoadingService));
			_marketNewPositionAnalysisService = marketNewPositionAnalysisService ?? throw new ArgumentNullException(nameof(marketNewPositionAnalysisService));
			_configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
			_loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
		}

		public async Task<IList<CurrencyPair>> GetActiveCurrencyPairs()
		{
			await _stockSocketConnector.ConnectAsync();

			var tradingSettings = _configurationService.GetTradingSettings();
			var allCurrencyPairs = await _stockSocketConnector.GetCurrencyPairs();
			var tradingCurrencyPairs = allCurrencyPairs.Where(item => tradingSettings.QuoteCurrencies.Any(currencyId => String.Equals(item.BaseCurrencyId, currencyId, StringComparison.OrdinalIgnoreCase)));

			return tradingCurrencyPairs.ToList();
		}

		public void StartMonitoring()
		{
			var cancelTokenSource = new CancellationTokenSource();
			try
			{
				var tradingSettings = _configurationService.GetTradingSettings();
				tradingSettings.Period = CandlePeriod.Minute5;
				tradingSettings.Moment = null;
				tradingSettings.BaseOrderSide = OrderSide.Buy;
				_configurationService.UpdateTradingSettings(tradingSettings);

				_stockSocketConnector.ConnectAsync().Wait(cancelTokenSource.Token);
				_stockSocketConnector.SubscribeErrors(exception =>
				{
					OnException(new UnhandledExceptionEventArgs(exception, false));
					cancelTokenSource.Cancel();
				});

				Task.WaitAll(StartMonitoringInner(cancelTokenSource.Token));
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

		private async Task StartMonitoringInner(CancellationToken cancellationToken)
		{
			_stockSocketConnector.Connect();

			var tradingSettings = _configurationService.GetTradingSettings();
			var tradingCurrencyPairs = await GetActiveCurrencyPairs();

			var periodsForAnalysis = new[]
			{
				tradingSettings.Period.GetLowerFramePeriod(),
				tradingSettings.Period,
				tradingSettings.Period.GetHigherFramePeriod()
			};

			foreach (var currencyPair in tradingCurrencyPairs)
			{
				await _candleLoadingService.InitSubscription(currencyPair.Id, periodsForAnalysis);
				_candleLoadingService.CandlesUpdated += (o, e) =>
				{
					if (e.Period != tradingSettings.Period)
						return;

					CheckOutForNewPosition(currencyPair);
				};
			}

			WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle });

			await _stockSocketConnector.Disconnect();
		}

		private void CheckOutForNewPosition(CurrencyPair currencyPair)
		{
			var marketInfo = _marketNewPositionAnalysisService.ProcessMarketPosition(currencyPair);
			if (marketInfo.PositionType != NewMarketPositionType.Wait && !_suggestedCurrencyPairs.ContainsKey(currencyPair.Id))
			{
				if (_suggestedCurrencyPairs.TryAdd(currencyPair.Id, currencyPair))
					OnSuggestedCurrencyPairsUpdated();
			}
			else if (marketInfo.PositionType == NewMarketPositionType.Wait && _suggestedCurrencyPairs.ContainsKey(currencyPair.Id))
			{
				if (_suggestedCurrencyPairs.TryRemove(currencyPair.Id, out var _))
					OnSuggestedCurrencyPairsUpdated();
			}
		}

		private void OnSuggestedCurrencyPairsUpdated()
		{
			SuggestedCurrencyPairsUpdated?.Invoke(this, new CurrencyPairsEventArgs(_suggestedCurrencyPairs.Values.ToList()));
		}

		private void OnException(UnhandledExceptionEventArgs e)
		{
			Exception?.Invoke(this, e);
		}
	}
}
