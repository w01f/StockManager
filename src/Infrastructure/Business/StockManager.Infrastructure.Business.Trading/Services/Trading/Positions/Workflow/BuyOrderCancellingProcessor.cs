using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Orders;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Logging.Common.Enums;
using StockManager.Infrastructure.Utilities.Logging.Models.Orders;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.Workflow
{
	class BuyOrderCancellingProcessor : ITradingPositionStateProcessor
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly ITradingDataConnector _tradingDataConnector;
		private readonly IOrdersService _ordersService;
		private readonly ILoggingService _loggingService;

		public BuyOrderCancellingProcessor(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
			ITradingDataConnector tradingDataConnector,
			IOrdersService ordersService,
			ILoggingService loggingService)
		{
			_orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
			_tradingDataConnector = tradingDataConnector ?? throw new ArgumentNullException(nameof(tradingDataConnector));
			_ordersService = ordersService ?? throw new ArgumentNullException(nameof(ordersService));
			_loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
		}

		public bool IsAllowToProcess(TradingPosition currentState, TradingPosition nextState)
		{
			return (currentState.OpenPositionOrder.OrderStateType == OrderStateType.Filled &&
					currentState.ClosePositionOrder.OrderStateType == OrderStateType.Pending &&
					currentState.StopLossOrder.OrderStateType == OrderStateType.Pending)
					&&
					(nextState.OpenPositionOrder.OrderStateType == OrderStateType.Cancelled &&
					nextState.ClosePositionOrder.OrderStateType == OrderStateType.Pending &&
					nextState.ClosePositionOrder.OrderStateType == OrderStateType.Pending);
		}

		public async Task<TradingPosition> ProcessTradingPositionChanging(TradingPosition currentState,
			TradingPosition nextState,
			bool syncWithStock,
			Action<PositionChangedEventArgs> onPositionChangedCallback)
		{
			if (syncWithStock)
			{
				try
				{
					nextState.OpenPositionOrder = await _ordersService.CancelOrder(currentState.OpenPositionOrder);
				}
				catch (ConnectorException e)
				{
					if (e.Message?.Contains("Order not found") ?? false)
					{
						var activeOrders = await _tradingDataConnector.GetActiveOrders(currentState.OpenPositionOrder.CurrencyPair);
						var serverSideOpenPositionOrder = activeOrders.FirstOrDefault(order => order.ClientId == currentState.OpenPositionOrder.ClientId) ??
														await _tradingDataConnector.GetOrderFromHistory(currentState.OpenPositionOrder.ClientId, currentState.OpenPositionOrder.CurrencyPair);
						nextState.OpenPositionOrder.SyncWithAnotherOrder(serverSideOpenPositionOrder);
					}
					else
						throw;
				}
			}

			var openOrderEntity = _orderRepository.Get(nextState.OpenPositionOrder.Id);
			if (openOrderEntity == null)
				throw new BusinessException("Order was not found in storage")
				{
					Details = $"Open position order: {JsonConvert.SerializeObject(nextState.OpenPositionOrder)}"
				};
			_orderRepository.Update(nextState.OpenPositionOrder.ToEntity(openOrderEntity));
			_loggingService.LogAction(nextState.OpenPositionOrder.ToLogAction(nextState.OpenPositionOrder.OrderStateType == OrderStateType.Cancelled ?
				OrderActionType.Cancel :
				OrderActionType.Update));

			if (nextState.OpenPositionOrder.OrderStateType == OrderStateType.Cancelled)
				onPositionChangedCallback?.Invoke(new PositionChangedEventArgs(TradingEventType.PositionCancelled, nextState.CurrencyPairId));

			return nextState;
		}
	}
}
