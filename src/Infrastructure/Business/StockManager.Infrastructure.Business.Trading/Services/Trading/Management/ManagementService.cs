using System;
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
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Utilities.Logging.Models.Errors;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Management
{
	public class ManagementService
	{
		private readonly IMarketNewPositionAnalysisService _marketNewPositionAnalysisService;
		private readonly IMarketPendingPositionAnalysisService _marketPendingPositionAnalysisService;
		private readonly IMarketOpenPositionAnalysisService _marketOpenPositionAnalysisService;
		private readonly IOrdersService _orderService;
		private readonly ConfigurationService _configurationService;
		private readonly ILoggingService _loggingService;

		public ManagementService(IMarketNewPositionAnalysisService marketNewPositionAnalysisService,
			IMarketPendingPositionAnalysisService marketPendingPositionAnalysisService,
			IMarketOpenPositionAnalysisService marketOpenPositionAnalysisService,
			IOrdersService orderService,
			ConfigurationService configurationService,
			ILoggingService loggingService)
		{
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
				var settings = _configurationService.GetTradingSettings();

				await _orderService.SyncOrders();

				var activeOrderPair = await _orderService.GetActiveOrder(settings.CurrencyPairId);

				if (activeOrderPair != null)
				{
					if (activeOrderPair.IsOpenPosition)
					{
						var marketInfo = await _marketOpenPositionAnalysisService.ProcessMarketPosition(activeOrderPair);
						if (marketInfo.PositionType == OpenMarketPositionType.FixStopLoss)
						{
							activeOrderPair.ApplyOrderChanges((UpdateClosePositionInfo)marketInfo);
							await _orderService.UpdateOrder(activeOrderPair);
						}
					}
					else if (activeOrderPair.IsPendingPosition)
					{
						var marketInfo = await _marketPendingPositionAnalysisService.ProcessMarketPosition(activeOrderPair);
						if (marketInfo.PositionType == PendingMarketPositionType.UpdateOrder)
						{
							activeOrderPair.ApplyOrderChanges((UpdateOrderInfo)marketInfo);
							await _orderService.UpdateOrder(activeOrderPair);
						}
						else if (marketInfo.PositionType == PendingMarketPositionType.CancelOrder)
							await _orderService.CancelOrder(activeOrderPair);
					}
					else
						throw new BusinessException("Unexpected order pair state")
						{
							Details = String.Format("Order pair: {0}", JsonConvert.SerializeObject(activeOrderPair))
						};
				}
				else
				{
					var marketInfo = await _marketNewPositionAnalysisService.ProcessMarketPosition();
					if (marketInfo.PositionType != NewMarketPositionType.Wait)
						await _orderService.OpenOrder((NewOrderPositionInfo)marketInfo);
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
