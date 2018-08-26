using System;
using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Orders;
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
		private readonly ILoggingService _loggingService;

		public ManagementService(IMarketNewPositionAnalysisService marketNewPositionAnalysisService,
			IMarketPendingPositionAnalysisService marketPendingPositionAnalysisService,
			IMarketOpenPositionAnalysisService marketOpenPositionAnalysisService,
			IOrdersService orderService,
			ILoggingService loggingService)
		{
			_marketNewPositionAnalysisService = marketNewPositionAnalysisService;
			_marketPendingPositionAnalysisService = marketPendingPositionAnalysisService;
			_marketOpenPositionAnalysisService = marketOpenPositionAnalysisService;
			_orderService = orderService;
			_loggingService = loggingService;
		}

		public async Task RunTradingIteration(TradingSettings settings)
		{
			try
			{
				await _orderService.SyncOrders(settings);

				var activeOrderPair = await _orderService.GetActiveOrder(settings.CurrencyPairId);

				if (activeOrderPair != null)
				{
					if (activeOrderPair.IsOpenPosition)
					{
						var marketInfo = await _marketOpenPositionAnalysisService.ProcessMarketPosition(settings, activeOrderPair);
						if (marketInfo.PositionType == OpenMarketPositionType.FixStopLoss)
						{
							activeOrderPair.ApplyOrderChanges((UpdateStopLossInfo)marketInfo);
							await _orderService.UpdateOrder(activeOrderPair, settings);
						}
					}
					else if (activeOrderPair.IsPendingPosition)
					{
						var marketInfo = await _marketPendingPositionAnalysisService.ProcessMarketPosition(settings, activeOrderPair);
						if (marketInfo.PositionType == PendingMarketPositionType.UpdateOrder)
						{
							activeOrderPair.ApplyOrderChanges((UpdateOrderInfo)marketInfo);
							await _orderService.UpdateOrder(activeOrderPair, settings);
						}
						else if (marketInfo.PositionType == PendingMarketPositionType.CancelOrder)
							await _orderService.CancelOrder(activeOrderPair);
					}
					else
						throw new ArgumentException("Unexpected order pair state");
				}
				else
				{
					var marketInfo = await _marketNewPositionAnalysisService.ProcessMarketPosition(settings);
					if (marketInfo.PositionType != NewMarketPositionType.Wait)
						await _orderService.OpenOrder((NewOrderPositionInfo)marketInfo, settings);
				}
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
