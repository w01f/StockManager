using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Orders;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Utilities.Logging.Models.Errors;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Management
{
	public class ManagementService
	{
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly ITradingDataConnector _tradingDataConnector;
		private readonly IMarketNewPositionAnalysisService _marketNewPositionAnalysisService;
		private readonly IMarketPendingPositionAnalysisService _marketPendingPositionAnalysisService;
		private readonly IMarketOpenPositionAnalysisService _marketOpenPositionAnalysisService;
		private readonly IOrdersService _orderService;
		private readonly ConfigurationService _configurationService;
		private readonly ILoggingService _loggingService;

		public ManagementService(IMarketDataConnector marketDataConnector,
			ITradingDataConnector tradingDataConnector,
			IMarketNewPositionAnalysisService marketNewPositionAnalysisService,
			IMarketPendingPositionAnalysisService marketPendingPositionAnalysisService,
			IMarketOpenPositionAnalysisService marketOpenPositionAnalysisService,
			IOrdersService orderService,
			ConfigurationService configurationService,
			ILoggingService loggingService)
		{
			_marketDataConnector = marketDataConnector;
			_tradingDataConnector = tradingDataConnector;
			_marketNewPositionAnalysisService = marketNewPositionAnalysisService;
			_marketPendingPositionAnalysisService = marketPendingPositionAnalysisService;
			_marketOpenPositionAnalysisService = marketOpenPositionAnalysisService;
			_orderService = orderService;
			_configurationService = configurationService;
			_loggingService = loggingService;
		}

		public async Task RunTradingIteration()
		{
			try
			{
				await _orderService.SyncOrders();

				var activeOrderPairs = await _orderService.GetActiveOrders();

				if (activeOrderPairs.Any())
				{
					foreach (var activeOrderPair in activeOrderPairs)
					{
						if (activeOrderPair.IsOpenPosition)
						{
							var marketInfo = await _marketOpenPositionAnalysisService.ProcessMarketPosition(activeOrderPair);
							if (marketInfo.PositionType == OpenMarketPositionType.UpdateOrder)
							{
								activeOrderPair.ApplyOrderChanges((UpdateClosePositionInfo)marketInfo);
								await _orderService.UpdatePosition(activeOrderPair);
							}
							else if (marketInfo.PositionType == OpenMarketPositionType.FixStopLoss)
							{
								activeOrderPair.ApplyOrderChanges((FixStopLossInfo)marketInfo);
								await _orderService.UpdatePosition(activeOrderPair);
							}
						}
						else if (activeOrderPair.IsPendingPosition)
						{
							var marketInfo = await _marketPendingPositionAnalysisService.ProcessMarketPosition(activeOrderPair);
							if (marketInfo.PositionType == PendingMarketPositionType.UpdateOrder)
							{
								activeOrderPair.ApplyOrderChanges((UpdateOrderInfo)marketInfo);
								await _orderService.UpdatePosition(activeOrderPair);
							}
							else if (marketInfo.PositionType == PendingMarketPositionType.CancelOrder)
								await _orderService.CancelPosition(activeOrderPair);
						}
						else
							throw new BusinessException("Unexpected order pair state")
							{
								Details = String.Format("Order pair: {0}", JsonConvert.SerializeObject(activeOrderPair))
							};
					}
				}

				{
					var tradingSettings = _configurationService.GetTradingSettings();

					var allCurrencyPairs = await _marketDataConnector.GetCurrensyPairs();

					var baseCurrencyPairs = allCurrencyPairs
						.Where(item => tradingSettings.QuoteCurrencies.Any(currencyId =>
										   String.Equals(item.QuoteCurrencyId, currencyId, StringComparison.OrdinalIgnoreCase)) &&
									   !activeOrderPairs.SelectMany(orderPair => new[]
										   {
											   orderPair.OpenPositionOrder.CurrencyPair.BaseCurrencyId,
											   orderPair.OpenPositionOrder.CurrencyPair.QuoteCurrencyId
										   })
										   .Any(currencyId => String.Equals(currencyId, item.BaseCurrencyId) ||
															  String.Equals(currencyId, item.QuoteCurrencyId))
						);

					var allTickers = await _marketDataConnector.GetTickers();
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
						var tradingBalance = await _tradingDataConnector.GetTradingBallnce(currencyPair.QuoteCurrencyId);
						if (tradingBalance?.Available <= 0)
							continue;

						var marketInfo = await _marketNewPositionAnalysisService.ProcessMarketPosition(currencyPair);
						if (marketInfo.PositionType != NewMarketPositionType.Wait)
						{
							await _orderService.OpenPosition((NewOrderPositionInfo)marketInfo);
							break;
						}
					}
				}
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
			catch (ParseResponceException e)
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

		public void TrackEvent(string message)
		{
			throw new NotImplementedException();
		}
	}
}
