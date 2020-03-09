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
	class StopLossOrderUpdatingProcessor : ITradingPositionStateProcessor
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly IOrdersService _ordersService;
		private readonly ILoggingService _loggingService;

		public StopLossOrderUpdatingProcessor(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
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
					currentState.ClosePositionOrder.OrderStateType == OrderStateType.Pending &&
					currentState.StopLossOrder.OrderStateType == OrderStateType.Suspended)
				&&
					(nextState.OpenPositionOrder.OrderStateType == OrderStateType.Filled &&
					nextState.ClosePositionOrder.OrderStateType == OrderStateType.Pending &&
					nextState.StopLossOrder.OrderStateType == OrderStateType.Suspended);
		}

		public async Task<TradingPosition> ProcessTradingPositionChanging(TradingPosition currentState,
			TradingPosition nextState,
			bool syncWithStock,
			Action<PositionChangedEventArgs> onPositionChangedCallback)
		{
			if (syncWithStock)
				await _ordersService.CancelOrder(currentState.StopLossOrder);

			nextState.StopLossOrder.ClientId =  Guid.NewGuid();
			nextState.StopLossOrder = await _ordersService.CreateSellMarketOrder(nextState.StopLossOrder);

			var stopLossOrderEntity = _orderRepository.Get(nextState.StopLossOrder.Id);
			if (stopLossOrderEntity == null)
				throw new BusinessException("Order was not found in storage")
				{
					Details = $"Stop loss order: {JsonConvert.SerializeObject(nextState.StopLossOrder)}"
				};
			_orderRepository.Update(nextState.StopLossOrder.ToEntity(stopLossOrderEntity));
			_loggingService.LogAction(nextState.StopLossOrder.ToLogAction(OrderActionType.Update));

			return nextState;
		}
	}
}
