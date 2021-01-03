using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Orders;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Utilities.Logging.Common.Enums;
using StockManager.Infrastructure.Utilities.Logging.Models.Orders;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.Workflow
{
	class OpenPositionProcessor : ITradingPositionStateProcessor
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly IOrdersService _ordersService;
		private readonly ILoggingService _loggingService;

		public OpenPositionProcessor(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
			IOrdersService ordersService,
			ILoggingService loggingService)
		{
			_orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
			_ordersService = ordersService ?? throw new ArgumentNullException(nameof(ordersService));
			_loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
		}

		public bool IsAllowToProcess(TradingPosition currentState, TradingPosition nextState)
		{
			return currentState == null;
		}

		public async Task<TradingPosition> ProcessTradingPositionChanging(TradingPosition currentState,
			TradingPosition nextState,
			bool syncWithStock,
			Action<PositionChangedEventArgs> onPositionChangedCallback)
		{
			nextState.OpenPositionOrder = await _ordersService.CreateBuyLimitOrder(nextState.OpenPositionOrder);

			nextState.ClosePositionOrder.ParentClientId = nextState.OpenPositionOrder.ClientId;
			nextState.StopLossOrder.ParentClientId = nextState.OpenPositionOrder.ClientId;

			_orderRepository.Insert(nextState.OpenPositionOrder.ToEntity());
			_orderRepository.Insert(nextState.ClosePositionOrder.ToEntity());
			_orderRepository.Insert(nextState.StopLossOrder.ToEntity());

			var storedOrderPair = _orderRepository.GetAll()
				.ToList()
				.GroupOrders()
				.SingleOrDefault(orderPair => orderPair.Item1.ClientId == nextState.OpenPositionOrder.ClientId);
			if (storedOrderPair == null)
				throw new BusinessException("Order was not found in storage")
				{
					Details = $"Open position order: {JsonConvert.SerializeObject(nextState.OpenPositionOrder)}"
				};

			nextState.OpenPositionOrder.Id = storedOrderPair.Item1.Id;
			nextState.ClosePositionOrder.Id = storedOrderPair.Item2.Id;
			nextState.StopLossOrder.Id = storedOrderPair.Item3.Id;

			_loggingService.LogAction(nextState.OpenPositionOrder.ToLogAction(OrderActionType.Create));

			onPositionChangedCallback?.Invoke(new PositionChangedEventArgs(TradingEventType.NewPosition, nextState));

			return nextState;
		}
	}
}
