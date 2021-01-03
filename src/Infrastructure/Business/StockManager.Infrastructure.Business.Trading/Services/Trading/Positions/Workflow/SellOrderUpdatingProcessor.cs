using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Orders;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Utilities.Logging.Common.Enums;
using StockManager.Infrastructure.Utilities.Logging.Models.Orders;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.Workflow
{
	class SellOrderUpdatingProcessor : ITradingPositionStateProcessor
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly IOrdersService _ordersService;
		private readonly ILoggingService _loggingService;

		public SellOrderUpdatingProcessor(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
			IOrdersService ordersService,
			ILoggingService loggingService)
		{
			_orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
			_ordersService = ordersService ?? throw new ArgumentNullException(nameof(ordersService));
			_loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
		}

		public bool IsAllowToProcess(TradingPosition currentState, TradingPosition nextState)
		{
			return (currentState.OpenPositionOrder.OrderStateType == OrderStateType.Filled &&
					(currentState.ClosePositionOrder.OrderStateType == OrderStateType.Pending ||
						currentState.ClosePositionOrder.OrderStateType == OrderStateType.Suspended ||
						currentState.ClosePositionOrder.OrderStateType == OrderStateType.New))
					&&
					(nextState.OpenPositionOrder.OrderStateType == OrderStateType.Filled &&
					(nextState.ClosePositionOrder.OrderStateType == OrderStateType.Suspended ||
						nextState.ClosePositionOrder.OrderStateType == OrderStateType.New ||
						nextState.ClosePositionOrder.OrderStateType == OrderStateType.PartiallyFilled));
		}

		public async Task<TradingPosition> ProcessTradingPositionChanging(TradingPosition currentState,
			TradingPosition nextState,
			bool syncWithStock,
			Action<PositionChangedEventArgs> onPositionChangedCallback)
		{
			if (syncWithStock)
			{
				if (currentState.ClosePositionOrder.OrderStateType == OrderStateType.Pending)
				{
					nextState.ClosePositionOrder = await _ordersService.CreateSellLimitOrder(nextState.ClosePositionOrder);
				}
				else
				{
					var newClientId = Guid.NewGuid();
					await _ordersService.RequestReplaceOrder(currentState.ClosePositionOrder, newClientId, () => currentState.IsAwaitingOrderUpdating = false);
					currentState.IsAwaitingOrderUpdating = true;

					//currentState.ClosePositionOrder.ClientId = newClientId;

					var closeOrderEntity = _orderRepository.Get(currentState.ClosePositionOrder.Id);
					if (closeOrderEntity == null)
						throw new BusinessException("Order was not found in storage")
						{
							Details = $"Close position order: {JsonConvert.SerializeObject(currentState.ClosePositionOrder)}"
						};
					_orderRepository.Update(currentState.ClosePositionOrder.ToEntity(closeOrderEntity));
					_loggingService.LogAction(currentState.ClosePositionOrder.ToLogAction(OrderActionType.Update));

					return currentState;
				}
			}

			{
				if (nextState.ClosePositionOrder.OrderStateType == OrderStateType.PartiallyFilled)
					nextState.ClosePositionOrder.OrderStateType = OrderStateType.New;

				var closeOrderEntity = _orderRepository.Get(nextState.ClosePositionOrder.Id);
				if (closeOrderEntity == null)
					throw new BusinessException("Order was not found in storage")
					{
						Details = $"Close position order: {JsonConvert.SerializeObject(nextState.ClosePositionOrder)}"
					};
				_orderRepository.Update(nextState.ClosePositionOrder.ToEntity(closeOrderEntity));
				_loggingService.LogAction(nextState.ClosePositionOrder.ToLogAction(currentState.ClosePositionOrder.OrderStateType == OrderStateType.Pending ?
					OrderActionType.Create :
					OrderActionType.Update));

				return nextState;
			}
		}
	}
}
