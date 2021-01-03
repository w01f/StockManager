using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StockManager.Domain.Core.Entities.Trading;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Utilities.Logging.Common.Enums;
using StockManager.Infrastructure.Utilities.Logging.Models.Orders;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.Workflow
{
	class ClosePositionProcessor : ITradingPositionStateProcessor
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly IRepository<OrderHistory> _orderHistoryRepository;
		private readonly ILoggingService _loggingService;

		public ClosePositionProcessor(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
			IRepository<OrderHistory> orderHistoryRepository,
			ILoggingService loggingService)
		{
			_orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
			_orderHistoryRepository = orderHistoryRepository ?? throw new ArgumentNullException(nameof(orderHistoryRepository));
			_loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
		}

		public bool IsAllowToProcess(TradingPosition currentState, TradingPosition nextState)
		{
			return (currentState.OpenPositionOrder.OrderStateType == OrderStateType.Cancelled && currentState.ClosePositionOrder.OrderStateType == OrderStateType.Pending && currentState.StopLossOrder.OrderStateType == OrderStateType.Pending) ||
				((currentState.OpenPositionOrder.OrderStateType == OrderStateType.Filled || currentState.OpenPositionOrder.OrderStateType == OrderStateType.Expired) &&
					(currentState.ClosePositionOrder.OrderStateType == OrderStateType.Filled || currentState.ClosePositionOrder.OrderStateType == OrderStateType.Cancelled) &&
					(currentState.StopLossOrder.OrderStateType == OrderStateType.Filled || currentState.StopLossOrder.OrderStateType == OrderStateType.Cancelled));
		}

		public async Task<TradingPosition> ProcessTradingPositionChanging(TradingPosition currentState,
			TradingPosition nextState,
			bool syncWithStock,
			Action<PositionChangedEventArgs> onPositionChangedCallback)
		{
			var openOrderEntity = _orderRepository.Get(currentState.OpenPositionOrder.Id);
			if (openOrderEntity == null)
				throw new BusinessException("Order was not found in storage")
				{
					Details = $"Open position order: {JsonConvert.SerializeObject(currentState.OpenPositionOrder)}"
				};
			_orderRepository.Delete(openOrderEntity);
			_loggingService.LogAction(currentState.OpenPositionOrder.ToLogAction(currentState.OpenPositionOrder.OrderStateType == OrderStateType.Filled ?
				OrderActionType.Fill :
				OrderActionType.Cancel));

			var closeOrderEntity = _orderRepository.Get(currentState.ClosePositionOrder.Id);
			if (closeOrderEntity == null)
				throw new BusinessException("Order was not found in storage")
				{
					Details = $"Close position order: {JsonConvert.SerializeObject(currentState.ClosePositionOrder)}"
				};
			_orderRepository.Delete(closeOrderEntity);
			_loggingService.LogAction(currentState.ClosePositionOrder.ToLogAction(currentState.ClosePositionOrder.OrderStateType == OrderStateType.Filled ?
				OrderActionType.Fill :
				OrderActionType.Cancel));

			var stopLossOrderEntity = _orderRepository.Get(currentState.StopLossOrder.Id);
			if (stopLossOrderEntity == null)
				throw new BusinessException("Order was not found in storage")
				{
					Details = $"Stop loss order: {JsonConvert.SerializeObject(currentState.StopLossOrder)}"
				};
			_orderRepository.Delete(stopLossOrderEntity);
			_loggingService.LogAction(currentState.StopLossOrder.ToLogAction(currentState.StopLossOrder.OrderStateType == OrderStateType.Filled ?
				OrderActionType.Fill :
				OrderActionType.Cancel));

			_orderRepository.SaveChanges();

			_orderHistoryRepository.Insert(currentState.OpenPositionOrder.ToHistory());
			_orderHistoryRepository.Insert(currentState.ClosePositionOrder.ToHistory());
			_orderHistoryRepository.Insert(currentState.StopLossOrder.ToHistory());

			currentState.IsClosedPosition = true;

			TradingEventType eventType;
			if (currentState.OpenPositionOrder.OrderStateType == OrderStateType.Cancelled)
				eventType = TradingEventType.PositionClosedDueCancel;
			else if (currentState.ClosePositionOrder.OrderStateType == OrderStateType.Filled)
				eventType = TradingEventType.PositionClosedSuccessfully;
			else
				eventType = TradingEventType.PositionClosedDueStopLoss;

			onPositionChangedCallback?.Invoke(new PositionChangedEventArgs(eventType, currentState));

			return await Task.FromResult<TradingPosition>(null);
		}
	}
}
