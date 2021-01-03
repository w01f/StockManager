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
	class StopLossOrderCancellingProcessor : ITradingPositionStateProcessor
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly IOrdersService _ordersService;
		private readonly ILoggingService _loggingService;

		public StopLossOrderCancellingProcessor(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
			IOrdersService ordersService,
			ILoggingService loggingService)
		{
			_orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
			_ordersService = ordersService ?? throw new ArgumentNullException(nameof(ordersService));
			_loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
		}

		public bool IsAllowToProcess(TradingPosition currentState, TradingPosition nextState)
		{
			return currentState.ClosePositionOrder.OrderStateType != OrderStateType.Filled &&
				(nextState.StopLossOrder.OrderStateType == OrderStateType.Cancelled || nextState.StopLossOrder.OrderStateType == OrderStateType.Expired);
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
					nextState.StopLossOrder = await _ordersService.CancelOrder(currentState.StopLossOrder);
				}
				catch (ConnectorException e)
				{
					if (e.Message?.Contains("Order not found") ?? false)
					{
						var activeOrders = await _ordersService.GetActiveOrders(currentState.StopLossOrder.CurrencyPair);
						var serverSideStopLossOrder = activeOrders.FirstOrDefault(order => order.ClientId == currentState.StopLossOrder.ClientId) ??
													await _ordersService.GetOrderFromHistory(currentState.StopLossOrder.ClientId, currentState.StopLossOrder.CurrencyPair);

						if (serverSideStopLossOrder != null)
						{
							if (serverSideStopLossOrder.OrderStateType == OrderStateType.Filled)
							{
								nextState.StopLossOrder.SyncWithAnotherOrder(serverSideStopLossOrder);
								var nextProcessor = new StopLossOrderFillingProcessor(_orderRepository, _ordersService, _loggingService);
								return await nextProcessor.ProcessTradingPositionChanging(currentState, nextState, true, onPositionChangedCallback);
							}
							
							if (serverSideStopLossOrder.OrderStateType == OrderStateType.Cancelled || serverSideStopLossOrder.OrderStateType == OrderStateType.Expired)
								nextState.StopLossOrder.SyncWithAnotherOrder(serverSideStopLossOrder);
							else
								throw;
						}
						else
							throw;
					}
					else
						throw;
				}
			}

			nextState.StopLossOrder.OrderStateType = OrderStateType.Cancelled;

			if (currentState.ClosePositionOrder.OrderStateType != OrderStateType.Filled)
			{
				if (!(currentState.ClosePositionOrder.OrderStateType == OrderStateType.Pending ||
					currentState.ClosePositionOrder.OrderStateType == OrderStateType.Cancelled ||
					currentState.ClosePositionOrder.OrderStateType == OrderStateType.Expired))
					try
					{
						await _ordersService.CancelOrder(currentState.ClosePositionOrder);
					}
					catch
					{
						// ignored
					}
					finally
					{
						nextState.ClosePositionOrder.OrderStateType = OrderStateType.Cancelled;
					}

				var immediateCloseOrder = new Order
				{
					ClientId = Guid.NewGuid(),
					CurrencyPair = currentState.ClosePositionOrder.CurrencyPair,
					Role = OrderRoleType.ClosePosition,
					OrderSide = currentState.ClosePositionOrder.OrderSide,
					OrderType = OrderType.Market,
					OrderStateType = OrderStateType.New,
					TimeInForce = OrderTimeInForceType.GoodTillCancelled
				};

				var serverSideOrder = await _ordersService.CreateSellMarketOrder(immediateCloseOrder);
				if (serverSideOrder.OrderStateType != OrderStateType.Filled)
					throw new BusinessException("Unexpected order state found")
					{
						Details = $"Close position market order: {JsonConvert.SerializeObject(serverSideOrder)}"
					};
			}

			var stopLossOrderEntity = _orderRepository.Get(nextState.StopLossOrder.Id);
			if (stopLossOrderEntity == null)
				throw new BusinessException("Order was not found in storage")
				{
					Details = $"Stop loss order: {JsonConvert.SerializeObject(nextState.StopLossOrder)}"
				};
			_orderRepository.Update(nextState.StopLossOrder.ToEntity(stopLossOrderEntity));
			_loggingService.LogAction(nextState.ClosePositionOrder.ToLogAction(OrderActionType.Cancel));

			return nextState;
		}
	}
}
