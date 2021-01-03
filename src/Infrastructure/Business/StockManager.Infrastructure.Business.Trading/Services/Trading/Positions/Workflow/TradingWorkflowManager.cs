using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Entities.Trading;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Orders;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.Workflow
{
	public class TradingWorkflowManager
	{
		private readonly IList<ITradingPositionStateProcessor> _workflowProcessors = new List<ITradingPositionStateProcessor>();

		public TradingWorkflowManager(IRepository<Order> orderRepository,
			IRepository<OrderHistory> orderHistoryRepository,
			IOrdersService ordersService,
			ILoggingService loggingService)
		{
			_workflowProcessors.Add(new OpenPositionProcessor(orderRepository, ordersService, loggingService));
			_workflowProcessors.Add(new BuyOrderUpdatingProcessor(orderRepository, ordersService, loggingService));
			_workflowProcessors.Add(new BuyOrderCancellingProcessor(orderRepository, ordersService, loggingService));
			_workflowProcessors.Add(new BuyOrderFillingProcessor(orderRepository, ordersService, loggingService));
			_workflowProcessors.Add(new SellOrderUpdatingProcessor(orderRepository, ordersService, loggingService));
			_workflowProcessors.Add(new SellOrderCancellingProcessor(orderRepository, ordersService, loggingService));
			_workflowProcessors.Add(new SellOrderFillingProcessor(orderRepository, ordersService, loggingService));
			_workflowProcessors.Add(new StopLossOrderUpdatingProcessor(orderRepository, ordersService, loggingService));
			_workflowProcessors.Add(new StopLossOrderCancellingProcessor(orderRepository, ordersService, loggingService));
			_workflowProcessors.Add(new StopLossOrderFillingProcessor(orderRepository, ordersService, loggingService));
			_workflowProcessors.Add(new ClosePositionProcessor(orderRepository, orderHistoryRepository, loggingService));
		}

		public async Task<TradingPosition> UpdateTradingPositionState(TradingPosition currentState,
			TradingPosition nextState,
			bool syncWithStock,
			Action<PositionChangedEventArgs> onPositionChangedCallback)
		{
			ITradingPositionStateProcessor selectedProcessor;
			var completedProcessors = new List<ITradingPositionStateProcessor>();
			var latestCurrentState = currentState;
			do
			{
				selectedProcessor = null;
				foreach (var stateProcessor in _workflowProcessors.Where(processor => !completedProcessors.Contains(processor)))
				{
					if (latestCurrentState == null || stateProcessor.IsAllowToProcess(latestCurrentState, nextState))
					{
						selectedProcessor = stateProcessor;
						completedProcessors.Add(selectedProcessor);
						break;
					}
				}
				if (selectedProcessor != null)
					latestCurrentState = await selectedProcessor.ProcessTradingPositionChanging(latestCurrentState,
						nextState,
						syncWithStock,
						onPositionChangedCallback);

				if(currentState==null)
					break;
			} while (latestCurrentState != null && selectedProcessor != null);

			return latestCurrentState;
		}
	}
}
