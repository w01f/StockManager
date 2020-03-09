using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Common;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Positions;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Utilities.Logging.Models.Errors;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers
{
	public class WebAPITradingController : ITradingController
	{
		private readonly IMarketDataRestConnector _marketDataRestConnector;
		private readonly ITradingDataConnector _tradingDataConnector;
		private readonly IMarketNewPositionAnalysisService _marketNewPositionAnalysisService;
		private readonly IMarketPendingPositionAnalysisService _marketPendingPositionAnalysisService;
		private readonly IMarketOpenPositionAnalysisService _marketOpenPositionAnalysisService;
		private readonly ITradingPositionService _tradingPositionService;
		private readonly ConfigurationService _configurationService;
		private readonly TradingEventsObserver _tradingEventsObserver;
		private readonly ILoggingService _loggingService;

		public WebAPITradingController(IMarketDataRestConnector marketDataRestConnector,
			ITradingDataConnector tradingDataConnector,
			IMarketNewPositionAnalysisService marketNewPositionAnalysisService,
			IMarketPendingPositionAnalysisService marketPendingPositionAnalysisService,
			IMarketOpenPositionAnalysisService marketOpenPositionAnalysisService,
			ITradingPositionService tradingPositionService,
			ConfigurationService configurationService,
			TradingEventsObserver tradingEventsObserver,
			ILoggingService loggingService)
		{
			_marketDataRestConnector = marketDataRestConnector;
			_tradingDataConnector = tradingDataConnector;
			_marketNewPositionAnalysisService = marketNewPositionAnalysisService;
			_marketPendingPositionAnalysisService = marketPendingPositionAnalysisService;
			_marketOpenPositionAnalysisService = marketOpenPositionAnalysisService;
			_tradingPositionService = tradingPositionService;
			_configurationService = configurationService;
			_tradingEventsObserver = tradingEventsObserver;
			_loggingService = loggingService;
		}

		public async Task StartTrading(CancellationToken cancellationToken)
		{
			try
			{
				var onPositionChangedCallback = new Action<PositionChangedEventArgs>(positionChangedEventArgs => _tradingEventsObserver.RaisePositionChanged(positionChangedEventArgs));

				await _tradingPositionService.SyncExistingPositionsWithStock(onPositionChangedCallback);

				var tradingPositions = await _tradingPositionService.GetOpenPositions();

				if (tradingPositions.Any())
				{
					foreach (var tradingPosition in tradingPositions)
					{
						if (tradingPosition.IsOpenPosition)
						{
							var marketInfo = await _marketOpenPositionAnalysisService.ProcessMarketPosition(tradingPosition);
							if (marketInfo.PositionType == OpenMarketPositionType.UpdateOrder)
								tradingPosition.ChangePosition((UpdateClosePositionInfo)marketInfo);
							else if (marketInfo.PositionType == OpenMarketPositionType.FixStopLoss)
								tradingPosition.ChangePosition((FixStopLossInfo)marketInfo);
							else if (marketInfo.PositionType == OpenMarketPositionType.Suspend)
								tradingPosition.ChangePosition((SuspendPositionInfo)marketInfo);

							if (marketInfo.PositionType != OpenMarketPositionType.Hold)
								await _tradingPositionService.UpdatePosition(tradingPosition, onPositionChangedCallback);
						}
						else if (tradingPosition.IsPendingPosition)
						{
							var marketInfo = await _marketPendingPositionAnalysisService.ProcessMarketPosition(tradingPosition);
							if (marketInfo.PositionType == PendingMarketPositionType.UpdateOrder)
								tradingPosition.ChangePosition((UpdateOrderInfo)marketInfo);
							else if (marketInfo.PositionType == PendingMarketPositionType.CancelOrder)
								tradingPosition.ChangePosition((CancelOrderInfo)marketInfo);

							if (marketInfo.PositionType != PendingMarketPositionType.Hold)
								await _tradingPositionService.UpdatePosition(tradingPosition, onPositionChangedCallback);
						}
						else
							throw new BusinessException("Unexpected position state")
							{
								Details = $"Order pair: {JsonConvert.SerializeObject(tradingPosition)}"
							};
					}
				}

				{
					var tradingSettings = _configurationService.GetTradingSettings();

					var allCurrencyPairs = await _marketDataRestConnector.GetCurrensyPairs();

					var baseCurrencyPairs = allCurrencyPairs
						.Where(item => tradingSettings.QuoteCurrencies.Any(currencyId =>
										String.Equals(item.QuoteCurrencyId, currencyId, StringComparison.OrdinalIgnoreCase)) &&
										!tradingPositions.Any(orderPair => String.Equals(orderPair.OpenPositionOrder.CurrencyPair.BaseCurrencyId, item.BaseCurrencyId))
						);

					var allTickers = await _marketDataRestConnector.GetTickers();
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

					foreach (var currencyPair in baseCurrencyPairs.Where(currencyPairItem => tradingTickers.Any(tickerItem =>
						String.Equals(tickerItem.CurrencyPairId, currencyPairItem.Id, StringComparison.OrdinalIgnoreCase))).ToList())
					{
						var tradingBalance = await _tradingDataConnector.GetTradingBalance(currencyPair.QuoteCurrencyId);
						if (tradingBalance?.Available <= 0)
							continue;

						var marketInfo = await _marketNewPositionAnalysisService.ProcessMarketPosition(currencyPair);
						if (marketInfo.PositionType != NewMarketPositionType.Wait)
						{
							var newPosition = await _tradingPositionService.OpenPosition((NewOrderPositionInfo)marketInfo, onPositionChangedCallback);
							_tradingEventsObserver.RaisePositionChanged(TradingEventType.NewPosition, newPosition.CurrencyPairId);
							break;
						}
					}
				}
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
				throw;
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
				throw;
			}
			catch (Exception e)
			{
				_loggingService.LogAction(new ErrorAction
				{
					ExceptionType = e.GetType().ToString(),
					Message = e.Message,
					StackTrace = e.StackTrace
				});
				throw;
			}
		}
	}
}
