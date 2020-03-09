using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Entities.Trading;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.Workflow;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions
{
	public class TradingPositionService : ITradingPositionService
	{
		private readonly IRepository<Order> _orderRepository;
		private readonly IMarketDataRestConnector _marketDataRestConnector;
		private readonly ITradingDataConnector _tradingDataConnector;
		private readonly TradingWorkflowManager _tradingWorkflowManager;
		private readonly ConfigurationService _configurationService;

		public TradingPositionService(IRepository<Order> orderRepository,
			IMarketDataRestConnector marketDataRestConnector,
			ITradingDataConnector tradingDataConnector,
			TradingWorkflowManager tradingWorkflowManager,
			ConfigurationService configurationService)
		{
			_orderRepository = orderRepository;
			_marketDataRestConnector = marketDataRestConnector ?? throw new ArgumentNullException(nameof(marketDataRestConnector));
			_tradingDataConnector = tradingDataConnector ?? throw new ArgumentNullException(nameof(tradingDataConnector));
			_tradingWorkflowManager = tradingWorkflowManager ?? throw new ArgumentNullException(nameof(orderRepository));
			_configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
		}

		public async Task SyncExistingPositionsWithStock(Action<PositionChangedEventArgs> onPositionChangedCallback)
		{
			var storedPositions = await GetOpenPositions();
			foreach (var currentPosition in storedPositions)
			{
				var currencyPair = currentPosition.OpenPositionOrder.CurrencyPair;
				var activeOrders = await _tradingDataConnector.GetActiveOrders(currencyPair);

				var updatedOpenPositionOrder = activeOrders.FirstOrDefault(order => order.ClientId == currentPosition.OpenPositionOrder.ClientId) ??
												await _tradingDataConnector.GetOrderFromHistory(currentPosition.OpenPositionOrder.ClientId, currencyPair) ??
												currentPosition.OpenPositionOrder;
				var updatedClosePositionOrder = activeOrders.FirstOrDefault(order => order.ClientId == currentPosition.ClosePositionOrder.ClientId) ??
													await _tradingDataConnector.GetOrderFromHistory(currentPosition.ClosePositionOrder.ClientId, currencyPair) ??
													currentPosition.ClosePositionOrder;
				var updatedStopLossOrder = activeOrders.FirstOrDefault(order => order.ClientId == currentPosition.StopLossOrder.ClientId) ??
											await _tradingDataConnector.GetOrderFromHistory(currentPosition.StopLossOrder.ClientId, currencyPair) ??
											currentPosition.StopLossOrder;

				var ordersTuple = Tuple.Create(updatedOpenPositionOrder, updatedClosePositionOrder, updatedStopLossOrder);
				var updatedPosition = ordersTuple.ToTradingPosition();

				await _tradingWorkflowManager.UpdateTradingPositionState(currentPosition, updatedPosition, false, onPositionChangedCallback);
			}
		}

		public async Task<IList<TradingPosition>> GetOpenPositions()
		{
			var storedOrderPairs = _orderRepository.GetAll().ToList().GroupOrders();

			var orderPairModels = new List<TradingPosition>();
			foreach (var orderPair in storedOrderPairs)
			{
				var currencyPair = await _marketDataRestConnector.GetCurrensyPair(orderPair.Item1.CurrencyPair);

				if (currencyPair == null)
					throw new BusinessException("Currency pair not found")
					{
						Details = $"Currency pair id: {orderPair.Item1.CurrencyPair}"
					};

				orderPairModels.Add(orderPair.ToTradingPosition(currencyPair));
			}

			return orderPairModels;
		}

		public async Task<TradingPosition> OpenPosition(NewOrderPositionInfo positionInfo, Action<PositionChangedEventArgs> onPositionChangedCallback)
		{
			var tradingSettings = _configurationService.GetTradingSettings();
			var currencyPair = await _marketDataRestConnector.GetCurrensyPair(positionInfo.CurrencyPairId);

			var position = TradingPositionHelper.CreatePosition(positionInfo, currencyPair, tradingSettings);

			position = await _tradingWorkflowManager.UpdateTradingPositionState(null, position, true, onPositionChangedCallback);
			return position;
		}

		public async Task<TradingPosition> UpdatePosition(TradingPosition nextPosition, Action<PositionChangedEventArgs> onPositionChangedCallback)
		{
			var currencyPair = await _marketDataRestConnector.GetCurrensyPair(nextPosition.CurrencyPairId);
			var currentPosition = _orderRepository.GetAll()
					.Where(entity => String.Equals(entity.CurrencyPair, nextPosition.CurrencyPairId, StringComparison.OrdinalIgnoreCase))
					.ToList()
					.GroupOrders()
					.Single()
					.ToTradingPosition(currencyPair);

			return await _tradingWorkflowManager.UpdateTradingPositionState(currentPosition, nextPosition, true, onPositionChangedCallback);
		}

		public async Task<TradingPosition> UpdatePosition(TradingPosition currentPosition, TradingPosition nextPosition, Action<PositionChangedEventArgs> onPositionChangedCallback)
		{
			return await _tradingWorkflowManager.UpdateTradingPositionState(currentPosition, nextPosition, true, onPositionChangedCallback);
		}
	}
}