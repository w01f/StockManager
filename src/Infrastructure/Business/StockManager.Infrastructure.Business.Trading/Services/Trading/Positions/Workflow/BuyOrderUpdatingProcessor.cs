using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Orders;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Common;
using StockManager.Infrastructure.Utilities.Logging.Common.Enums;
using StockManager.Infrastructure.Utilities.Logging.Models.Orders;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.Workflow
{
	class BuyOrderUpdatingProcessor : ITradingPositionStateProcessor
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly IOrdersService _ordersService;
		private readonly ILoggingService _loggingService;

		public BuyOrderUpdatingProcessor(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
			IOrdersService ordersService,
			ILoggingService loggingService)
		{
			_orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
			_ordersService = ordersService ?? throw new ArgumentNullException(nameof(ordersService));
			_loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
		}

		public bool IsAllowToProcess(TradingPosition currentState, TradingPosition nextState)
		{
			return ((currentState.OpenPositionOrder.OrderStateType == OrderStateType.New || currentState.OpenPositionOrder.OrderStateType == OrderStateType.Suspended) &&
					currentState.ClosePositionOrder.OrderStateType == OrderStateType.Pending &&
					currentState.StopLossOrder.OrderStateType == OrderStateType.Pending)
					&&
					((nextState.OpenPositionOrder.OrderStateType == OrderStateType.New || nextState.OpenPositionOrder.OrderStateType == OrderStateType.Suspended) &&
					nextState.ClosePositionOrder.OrderStateType == OrderStateType.Pending &&
					nextState.StopLossOrder.OrderStateType == OrderStateType.Pending);
		}

		public async Task<TradingPosition> ProcessTradingPositionChanging(TradingPosition currentState,
			TradingPosition nextState,
			bool syncWithStock,
			Action<PositionChangedEventArgs> onPositionChangedCallback)
		{
			if (syncWithStock)
				try
				{
					await _ordersService.CancelOrder(currentState.OpenPositionOrder);
				}
				catch (ConnectorException e)
				{
					if (e.Message?.Contains("Order not found") ?? false)
					{
						var activeOrders = await _ordersService.GetActiveOrders(currentState.OpenPositionOrder.CurrencyPair);
						var serverSideOpenPositionOrder = activeOrders.FirstOrDefault(order => order.ClientId == currentState.OpenPositionOrder.ClientId) ??
														await _ordersService.GetOrderFromHistory(currentState.OpenPositionOrder.ClientId, currentState.OpenPositionOrder.CurrencyPair);
						if (serverSideOpenPositionOrder != null)
						{
							if (serverSideOpenPositionOrder.OrderStateType == OrderStateType.Filled)
							{
								nextState.OpenPositionOrder.SyncWithAnotherOrder(serverSideOpenPositionOrder);
								var nextProcessor = new BuyOrderFillingProcessor(_orderRepository, _ordersService, _loggingService);
								return await nextProcessor.ProcessTradingPositionChanging(currentState, nextState, true, onPositionChangedCallback);
							}
							else
								nextState.OpenPositionOrder.SyncWithAnotherOrder(serverSideOpenPositionOrder);
						}
						else
							throw;
					}
					else
						throw;
				}

			nextState.OpenPositionOrder.ClientId = Guid.NewGuid();
			nextState.ClosePositionOrder.ParentClientId = nextState.OpenPositionOrder.ClientId;
			nextState.StopLossOrder.ParentClientId = nextState.OpenPositionOrder.ClientId;

			if (nextState.OpenPositionOrder.OrderStateType != OrderStateType.New &&
				nextState.OpenPositionOrder.StopPrice == null)
				throw new BusinessException("Unexpected order state found")
				{
					Details = $"Open position order: {JsonConvert.SerializeObject(nextState.OpenPositionOrder)}"
				};

			nextState.OpenPositionOrder = await _ordersService.CreateBuyLimitOrder(nextState.OpenPositionOrder);
			nextState.ClosePositionOrder.ParentClientId = nextState.OpenPositionOrder.ClientId;
			nextState.StopLossOrder.ParentClientId = nextState.OpenPositionOrder.ClientId;

			var openOrderEntity = _orderRepository.Get(nextState.OpenPositionOrder.Id);
			if (openOrderEntity == null)
				throw new BusinessException("Order was not found in storage")
				{
					Details = $"Open position order: {JsonConvert.SerializeObject(nextState.OpenPositionOrder)}"
				};
			_orderRepository.Update(nextState.OpenPositionOrder.ToEntity(openOrderEntity));
			_loggingService.LogAction(nextState.OpenPositionOrder.ToLogAction(OrderActionType.Update));

			var closeOrderEntity = _orderRepository.Get(nextState.ClosePositionOrder.Id);
			if (closeOrderEntity == null)
				throw new BusinessException("Order was not found in storage")
				{
					Details = $"Close position order: {JsonConvert.SerializeObject(nextState.ClosePositionOrder)}"
				};
			_orderRepository.Update(nextState.ClosePositionOrder.ToEntity(closeOrderEntity));


			var stopLossOrderEntity = _orderRepository.Get(nextState.StopLossOrder.Id);
			if (stopLossOrderEntity == null)
				throw new BusinessException("Order was not found in storage")
				{
					Details = $"Stop loss order: {JsonConvert.SerializeObject(nextState.StopLossOrder)}"
				};
			_orderRepository.Update(nextState.StopLossOrder.ToEntity(stopLossOrderEntity));

			return nextState;
		}
	}
}
