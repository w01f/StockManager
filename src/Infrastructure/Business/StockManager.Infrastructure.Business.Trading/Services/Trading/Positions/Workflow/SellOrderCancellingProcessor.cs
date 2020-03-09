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
	class SellOrderCancellingProcessor : ITradingPositionStateProcessor
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly IOrdersService _ordersService;
		private readonly ILoggingService _loggingService;

		public SellOrderCancellingProcessor(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
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
					(currentState.ClosePositionOrder.OrderStateType == OrderStateType.Suspended ||
						currentState.ClosePositionOrder.OrderStateType == OrderStateType.New ||
						currentState.ClosePositionOrder.OrderStateType == OrderStateType.PartiallyFilled) &&
					currentState.ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
					&&
					(nextState.OpenPositionOrder.OrderStateType == OrderStateType.Filled &&
					(nextState.ClosePositionOrder.OrderStateType == OrderStateType.Pending || nextState.ClosePositionOrder.OrderStateType == OrderStateType.Expired) &&
					nextState.ClosePositionOrder.OrderStateType == OrderStateType.Suspended);
		}

		public async Task<TradingPosition> ProcessTradingPositionChanging(TradingPosition currentState,
			TradingPosition nextState,
			bool syncWithStock,
			Action<PositionChangedEventArgs> onPositionChangedCallback)
		{
			if (syncWithStock)
				await _ordersService.CancelOrder(currentState.ClosePositionOrder);
			nextState.ClosePositionOrder.OrderStateType = OrderStateType.Pending;

			var closeOrderEntity = _orderRepository.Get(nextState.ClosePositionOrder.Id);
			if (closeOrderEntity == null)
				throw new BusinessException("Order was not found in storage")
				{
					Details = $"Close position order: {JsonConvert.SerializeObject(nextState.ClosePositionOrder)}"
				};
			_orderRepository.Update(nextState.ClosePositionOrder.ToEntity(closeOrderEntity));
			_loggingService.LogAction(nextState.ClosePositionOrder.ToLogAction(OrderActionType.Cancel));

			return nextState;
		}
	}
}
