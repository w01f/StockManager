using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Domain.Core.Entities.Trading;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Logging.Common.Enums;
using StockManager.Infrastructure.Utilities.Logging.Models.Orders;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Orders
{
	public class OrdersService : IOrdersService
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly IRepository<OrderHistory> _orderHistoryRepository;
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly ITradingDataConnector _tradingDataConnector;
		private readonly ILoggingService _loggingService;

		public OrdersService(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
			IRepository<OrderHistory> orderHistoryRepository,
			IMarketDataConnector marketDataConnector,
			ITradingDataConnector tradingDataConnector,
			ILoggingService loggingService)
		{
			_orderRepository = orderRepository;
			_orderHistoryRepository = orderHistoryRepository;
			_marketDataConnector = marketDataConnector;
			_tradingDataConnector = tradingDataConnector;
			_loggingService = loggingService;
		}

		public async Task SyncOrders(TradingSettings settings)
		{
			var currencyPair = await _marketDataConnector.GetCurrensyPair(settings.CurrencyPairId);

			var storedOrderEntity = GenerateOrderPairs(_orderRepository.GetAll()
				.Where(entity => String.Equals(entity.CurrencyPair, currencyPair.Id, StringComparison.OrdinalIgnoreCase))
				.ToList())
				.Single();

			var activeOrders = await _tradingDataConnector.GetActiveOrders(currencyPair);

			//TODO Check if new stop limit orders are active too
			//TODO Check if filled orders are active too
			foreach (var initialOrder in activeOrders.Where(order => order.OrderSide == settings.BaseOrderSide).ToList())
			{
				if (storedOrderEntity.Item1.ClientId != initialOrder.ClientId)
					throw new BusinessException("Undefined active order found");

				switch (initialOrder.OrderStateType)
				{
					case OrderStateType.New:
					case OrderStateType.PartiallyFilled:
						continue;
					default:
						throw new BusinessException("Unexpected order state found");
				}
			}

			{
				var initialOrder = activeOrders.FirstOrDefault(order => order.ClientId == storedOrderEntity.Item1.ClientId);
				if (initialOrder == null)
				{
					initialOrder = await _tradingDataConnector.GetOrder(storedOrderEntity.Item1.ClientId);
					if (initialOrder == null)
						throw new BusinessException("Initial order not found");

					if (initialOrder.OrderStateType != OrderStateType.Filled)
						throw new BusinessException("Unexpected order state found");

					var pairingOrder = await _tradingDataConnector.GetOrder(storedOrderEntity.Item2.ClientId);
					if (pairingOrder != null)
					{
						if (pairingOrder.OrderStateType != OrderStateType.Filled)
							throw new BusinessException("Unexpected order state found");

						_loggingService.LogAction(pairingOrder.ToLogAction(OrderActionType.Fill));

						_orderHistoryRepository.Insert(initialOrder.ToHistory());
						_orderHistoryRepository.Insert(pairingOrder.ToHistory());

						_orderRepository.Delete(storedOrderEntity.Item1);
						_orderRepository.Delete(storedOrderEntity.Item2);
						_orderRepository.SaveChanges();

						_loggingService.LogAction(initialOrder.ToLogAction(OrderActionType.History));
						_loggingService.LogAction(pairingOrder.ToLogAction(OrderActionType.History));
					}
					else
					{
						_orderRepository.Update(initialOrder.ToEntity(storedOrderEntity.Item1));
						_loggingService.LogAction(initialOrder.ToLogAction(OrderActionType.Fill));

						pairingOrder = storedOrderEntity.Item2.ToModel(currencyPair);

						var tradingBallnce = (await _tradingDataConnector.GetTradingBallnce())
							.FirstOrDefault(ballance =>
								String.Equals(ballance.CurrencyId, currencyPair.BaseCurrencyId, StringComparison.OrdinalIgnoreCase));

						if (tradingBallnce == null)
							throw new BusinessException("Trading balance is not available");

						pairingOrder.CalculateOrderAmount(tradingBallnce, settings);

						pairingOrder = await _tradingDataConnector.CreateOrder(pairingOrder);

						_orderRepository.Update(pairingOrder.ToEntity(storedOrderEntity.Item2));
						_loggingService.LogAction(pairingOrder.ToLogAction(OrderActionType.Update));
					}
				}
			}

			foreach (var pairingOrder in activeOrders.Where(order => order.OrderSide != settings.BaseOrderSide).ToList())
			{
				if (storedOrderEntity.Item2.ClientId != pairingOrder.ClientId)
					throw new BusinessException("Undefined active order found");

				switch (pairingOrder.OrderStateType)
				{
					case OrderStateType.New:
					case OrderStateType.PartiallyFilled:
						continue;
					default:
						throw new BusinessException("Unexpected order state found");
				}
			}
		}

		public async Task<OrderPair> GetActiveOrder(string currencyPairId)
		{
			var currencyPair = await _marketDataConnector.GetCurrensyPair(currencyPairId);
			var storedOrderPair = GenerateOrderPairs(_orderRepository.GetAll()
				.Where(entity => String.Equals(entity.CurrencyPair, currencyPairId, StringComparison.OrdinalIgnoreCase))
				.ToList())
				.Single();

			return storedOrderPair.ToModel(currencyPair);
		}

		public async Task OpenOrder(NewOrderPositionInfo positionInfo, TradingSettings settings)
		{
			var currencyPair = await _marketDataConnector.GetCurrensyPair(settings.CurrencyPairId);

			//TODO Separate price from stop price
			var initialOrder = new Infrastructure.Common.Models.Trading.Order();
			initialOrder.ClientId = Guid.NewGuid();
			initialOrder.CurrencyPair = currencyPair;
			initialOrder.OrderSide = settings.BaseOrderSide;
			initialOrder.OrderType = OrderType.StopLimit;
			initialOrder.OrderStateType = OrderStateType.New;
			initialOrder.Price = positionInfo.Price;
			initialOrder.StopPrice = positionInfo.StopPrice;

			var pairingOrder = new Infrastructure.Common.Models.Trading.Order();
			initialOrder.ClientId = Guid.NewGuid();
			initialOrder.ParentClientId = initialOrder.ClientId;
			initialOrder.CurrencyPair = currencyPair;
			initialOrder.OrderSide = settings.OppositeOrderSide;
			initialOrder.OrderType = OrderType.StopLimit;
			initialOrder.OrderStateType = OrderStateType.New;
			initialOrder.Price = positionInfo.StopLossPrice;
			initialOrder.StopPrice = positionInfo.StopLossStopPrice;

			var tradingBallnce = (await _tradingDataConnector.GetTradingBallnce())
				.FirstOrDefault(ballance => String.Equals(ballance.CurrencyId, currencyPair.QuoteCurrencyId, StringComparison.OrdinalIgnoreCase));

			if (tradingBallnce == null)
				throw new BusinessException("Trading balance is not available");

			initialOrder.CalculateOrderAmount(tradingBallnce, settings);

			initialOrder = await _tradingDataConnector.CreateOrder(initialOrder);

			_orderRepository.Insert(initialOrder.ToEntity());
			_orderRepository.Insert(pairingOrder.ToEntity());

			_loggingService.LogAction(initialOrder.ToLogAction(OrderActionType.Create));
			_loggingService.LogAction(pairingOrder.ToLogAction(OrderActionType.Create));
		}

		public async Task UpdateOrder(OrderPair orderPair, TradingSettings settings)
		{
			var storedOrderEntity = GenerateOrderPairs(_orderRepository.GetAll()
					.Where(entity => String.Equals(entity.CurrencyPair, orderPair.InitialOrder.CurrencyPair.Id, StringComparison.OrdinalIgnoreCase))
					.ToList())
				.Single();

			if (storedOrderEntity.Item1.OrderStateType != OrderStateType.Filled)
			{
				var initialOrder = await _tradingDataConnector.CancelOrder(orderPair.InitialOrder);
				if (initialOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed");

				orderPair.InitialOrder.ClientId = Guid.NewGuid();
				orderPair.StopLossOrder.ParentClientId = orderPair.InitialOrder.ClientId;

				var tradingBallnce = (await _tradingDataConnector.GetTradingBallnce())
					.FirstOrDefault(ballance => String.Equals(ballance.CurrencyId, orderPair.InitialOrder.CurrencyPair.QuoteCurrencyId, StringComparison.OrdinalIgnoreCase));

				if (tradingBallnce == null)
					throw new BusinessException("Trading balance is not available");

				orderPair.InitialOrder.CalculateOrderAmount(tradingBallnce, settings);

				orderPair.InitialOrder = await _tradingDataConnector.CreateOrder(orderPair.InitialOrder);

				_orderRepository.Update(orderPair.InitialOrder.ToEntity(storedOrderEntity.Item1));
				_orderRepository.Update(orderPair.StopLossOrder.ToEntity(storedOrderEntity.Item2));

				_loggingService.LogAction(orderPair.InitialOrder.ToLogAction(OrderActionType.Update));
				_loggingService.LogAction(orderPair.StopLossOrder.ToLogAction(OrderActionType.Update));
			}
			else if (storedOrderEntity.Item2.OrderStateType != OrderStateType.Filled)
			{
				var pairingOrder = await _tradingDataConnector.CancelOrder(orderPair.StopLossOrder);
				if (pairingOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed");

				orderPair.StopLossOrder.ClientId = Guid.NewGuid();

				var tradingBallnce = (await _tradingDataConnector.GetTradingBallnce())
					.FirstOrDefault(ballance => String.Equals(ballance.CurrencyId, orderPair.StopLossOrder.CurrencyPair.BaseCurrencyId, StringComparison.OrdinalIgnoreCase));

				if (tradingBallnce == null)
					throw new BusinessException("Trading balance is not available");

				orderPair.StopLossOrder.CalculateOrderAmount(tradingBallnce, settings);

				orderPair.StopLossOrder = await _tradingDataConnector.CreateOrder(orderPair.StopLossOrder);

				_orderRepository.Update(orderPair.StopLossOrder.ToEntity(storedOrderEntity.Item2));
				_loggingService.LogAction(orderPair.StopLossOrder.ToLogAction(OrderActionType.Update));
			}
			else
				throw new BusinessException("Unexpected order pair state found");
		}

		public async Task CancelOrder(OrderPair orderPair)
		{
			var storedOrderEntity = GenerateOrderPairs(_orderRepository.GetAll()
					.Where(entity => String.Equals(entity.CurrencyPair, orderPair.InitialOrder.CurrencyPair.Id, StringComparison.OrdinalIgnoreCase))
					.ToList())
				.Single();
			if (storedOrderEntity.Item1.OrderStateType != OrderStateType.Filled)
			{
				orderPair.InitialOrder = await _tradingDataConnector.CancelOrder(orderPair.InitialOrder);
				if (orderPair.InitialOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed");
			}
			else if (storedOrderEntity.Item2.OrderStateType != OrderStateType.Filled)
			{
				orderPair.StopLossOrder = await _tradingDataConnector.CancelOrder(orderPair.StopLossOrder);
				if (orderPair.StopLossOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed");
			}
			else
				throw new BusinessException("Unexpected order pair state found");

			_orderHistoryRepository.Insert(orderPair.InitialOrder.ToHistory());
			_orderHistoryRepository.Insert(orderPair.StopLossOrder.ToHistory());

			_orderRepository.Delete(storedOrderEntity.Item1);
			_orderRepository.Delete(storedOrderEntity.Item2);
			_orderRepository.SaveChanges();

			_loggingService.LogAction(orderPair.InitialOrder.ToLogAction(OrderActionType.Cancel));
			_loggingService.LogAction(orderPair.StopLossOrder.ToLogAction(OrderActionType.Cancel));

			_loggingService.LogAction(orderPair.InitialOrder.ToLogAction(OrderActionType.History));
			_loggingService.LogAction(orderPair.StopLossOrder.ToLogAction(OrderActionType.History));
		}

		private IList<Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order>> GenerateOrderPairs(IList<Domain.Core.Entities.Trading.Order> orderEntities)
		{
			var result = new List<Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order>>();
			foreach (var initialOrderEntity in orderEntities.Where(entity => entity.ClientId == Guid.Empty).ToList())
				result.Add(new Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order>(initialOrderEntity, orderEntities.Single(entity => entity.ParentClientId == initialOrderEntity.ClientId)));
			return result;
		}
	}
}