using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Entities.Trading;
using StockManager.Domain.Core.Enums;
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

			var storedOrders = _orderRepository.GetAll()
				.Where(entity => String.Equals(entity.CurrencyPair, currencyPair.Id, StringComparison.OrdinalIgnoreCase))
				.ToList();

			if (!storedOrders.Any())
				return;

			var storedOrderEntity = GenerateOrderPairs(storedOrders).Single();

			//TODO Check if filled orders are active too
			//TODO Check if stop order will cancel after closePosition order will filled
			//TODO Check if stop limit order still has stop price after it chnaged status form suspended to new
			var openPositionOrder = storedOrderEntity.Item1.ToModel(currencyPair);
			if (openPositionOrder.OrderStateType != OrderStateType.Filled)
			{
				var activeOrders = await _tradingDataConnector.GetActiveOrders(currencyPair);

				if (activeOrders.Any(order => order.ClientId != storedOrderEntity.Item1.ClientId &&
											  order.ClientId != storedOrderEntity.Item2.ClientId &&
											  order.ClientId != storedOrderEntity.Item3.ClientId))
					throw new BusinessException("Undefined orders found");

				openPositionOrder = activeOrders.FirstOrDefault(order => order.ClientId == storedOrderEntity.Item1.ClientId) ??
									await _tradingDataConnector.GetOrder(storedOrderEntity.Item1.ClientId);
				if (openPositionOrder == null)
					throw new BusinessException("Open position order not found");

				if (openPositionOrder.OrderStateType == OrderStateType.Filled)
				{
					_orderRepository.Update(openPositionOrder.ToEntity(storedOrderEntity.Item1));
					_loggingService.LogAction(openPositionOrder.ToLogAction(OrderActionType.Fill));
				}
				else
				{
					switch (openPositionOrder.OrderStateType)
					{
						case OrderStateType.Suspended:
						case OrderStateType.New:
							if (storedOrderEntity.Item1.OrderStateType != openPositionOrder.OrderStateType)
							{
								_orderRepository.Update(openPositionOrder.ToEntity(storedOrderEntity.Item1));
								_loggingService.LogAction(openPositionOrder.ToLogAction(OrderActionType.Update));
							}
							break;
						default:
							throw new BusinessException("Unexpected order state found");
					}
				}
			}

			if (openPositionOrder.OrderStateType == OrderStateType.Filled)
			{
				var closePositionOrder = await _tradingDataConnector.GetOrder(storedOrderEntity.Item2.ClientId);
				var stopLossOrder = await _tradingDataConnector.GetOrder(storedOrderEntity.Item3.ClientId);

				if (closePositionOrder == null)
				{
					if (stopLossOrder != null)
						throw new BusinessException("Unexpected order state found");

					closePositionOrder = storedOrderEntity.Item2.ToModel(currencyPair);
					stopLossOrder = storedOrderEntity.Item3.ToModel(currencyPair);

					var tradingBallnce = (await _tradingDataConnector.GetTradingBallnce())
						.FirstOrDefault(ballance =>
							String.Equals(ballance.CurrencyId, currencyPair.BaseCurrencyId, StringComparison.OrdinalIgnoreCase));

					if (tradingBallnce == null)
						throw new BusinessException("Trading balance is not available");

					closePositionOrder.CalculateOrderAmount(tradingBallnce, settings);
					stopLossOrder.CalculateOrderAmount(tradingBallnce, settings);

					closePositionOrder = await _tradingDataConnector.CreateOrder(closePositionOrder);
					stopLossOrder = await _tradingDataConnector.CreateOrder(stopLossOrder);

					_orderRepository.Update(closePositionOrder.ToEntity(storedOrderEntity.Item2));
					_orderRepository.Update(stopLossOrder.ToEntity(storedOrderEntity.Item3));
					_loggingService.LogAction(closePositionOrder.ToLogAction(OrderActionType.Update));
					_loggingService.LogAction(stopLossOrder.ToLogAction(OrderActionType.Update));
				}
				else
				{
					if (stopLossOrder == null)
						throw new BusinessException("Unexpected order state found");

					switch (closePositionOrder.OrderStateType)
					{
						case OrderStateType.Suspended:
						case OrderStateType.New:
							if (storedOrderEntity.Item2.OrderStateType != closePositionOrder.OrderStateType)
							{
								_orderRepository.Update(closePositionOrder.ToEntity(storedOrderEntity.Item2));
								_loggingService.LogAction(closePositionOrder.ToLogAction(OrderActionType.Update));
							}
							break;
						case OrderStateType.Filled:
							if (stopLossOrder.OrderStateType != OrderStateType.Cancelled)
								throw new BusinessException("Unexpected order state found");

							_loggingService.LogAction(closePositionOrder.ToLogAction(OrderActionType.Fill));
							_loggingService.LogAction(stopLossOrder.ToLogAction(OrderActionType.Cancel));

							_orderHistoryRepository.Insert(openPositionOrder.ToHistory());
							_orderHistoryRepository.Insert(closePositionOrder.ToHistory());
							_orderHistoryRepository.Insert(stopLossOrder.ToHistory());

							_orderRepository.Delete(storedOrderEntity.Item1);
							_orderRepository.Delete(storedOrderEntity.Item2);
							_orderRepository.Delete(storedOrderEntity.Item3);
							_orderRepository.SaveChanges();

							_loggingService.LogAction(openPositionOrder.ToLogAction(OrderActionType.History));
							_loggingService.LogAction(closePositionOrder.ToLogAction(OrderActionType.History));
							_loggingService.LogAction(stopLossOrder.ToLogAction(OrderActionType.History));
							break;
						case OrderStateType.Cancelled:
							if (stopLossOrder.OrderStateType != OrderStateType.Filled)
								throw new BusinessException("Unexpected order state found");

							_loggingService.LogAction(closePositionOrder.ToLogAction(OrderActionType.Cancel));
							_loggingService.LogAction(stopLossOrder.ToLogAction(OrderActionType.Fill));

							_orderHistoryRepository.Insert(openPositionOrder.ToHistory());
							_orderHistoryRepository.Insert(closePositionOrder.ToHistory());
							_orderHistoryRepository.Insert(stopLossOrder.ToHistory());

							_orderRepository.Delete(storedOrderEntity.Item1);
							_orderRepository.Delete(storedOrderEntity.Item2);
							_orderRepository.Delete(storedOrderEntity.Item3);
							_orderRepository.SaveChanges();

							_loggingService.LogAction(openPositionOrder.ToLogAction(OrderActionType.History));
							_loggingService.LogAction(closePositionOrder.ToLogAction(OrderActionType.History));
							_loggingService.LogAction(stopLossOrder.ToLogAction(OrderActionType.History));
							break;
						default:
							throw new BusinessException("Unexpected order state found");
					}
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

			var openPositionOrder = new Infrastructure.Common.Models.Trading.Order();
			openPositionOrder.ClientId = Guid.NewGuid();
			openPositionOrder.CurrencyPair = currencyPair;
			openPositionOrder.OrderSide = settings.BaseOrderSide;
			openPositionOrder.OrderType = OrderType.StopLimit;
			openPositionOrder.OrderStateType = OrderStateType.Suspended;
			openPositionOrder.Price = positionInfo.OpenPrice;
			openPositionOrder.StopPrice = positionInfo.OpenStopPrice;

			var closePositionOrder = new Infrastructure.Common.Models.Trading.Order();
			closePositionOrder.ClientId = Guid.NewGuid();
			closePositionOrder.ParentClientId = openPositionOrder.ClientId;
			closePositionOrder.CurrencyPair = currencyPair;
			closePositionOrder.OrderSide = settings.OppositeOrderSide;
			closePositionOrder.OrderType = OrderType.StopLimit;
			closePositionOrder.OrderStateType = OrderStateType.Suspended;
			closePositionOrder.Price = positionInfo.ClosePrice;
			closePositionOrder.StopPrice = positionInfo.CloseStopPrice;

			var stopLossOrder = new Infrastructure.Common.Models.Trading.Order();
			stopLossOrder.ClientId = Guid.NewGuid();
			stopLossOrder.ParentClientId = openPositionOrder.ClientId;
			stopLossOrder.CurrencyPair = currencyPair;
			stopLossOrder.OrderSide = settings.OppositeOrderSide;
			stopLossOrder.OrderType = OrderType.StopMarket;
			stopLossOrder.OrderStateType = OrderStateType.Suspended;
			stopLossOrder.Price = positionInfo.StopLossPrice;
			stopLossOrder.StopPrice = positionInfo.StopLossPrice;

			var tradingBallnce = (await _tradingDataConnector.GetTradingBallnce())
				.FirstOrDefault(ballance => String.Equals(ballance.CurrencyId, currencyPair.QuoteCurrencyId, StringComparison.OrdinalIgnoreCase));

			if (tradingBallnce == null)
				throw new BusinessException("Trading balance is not available");

			openPositionOrder.CalculateOrderAmount(tradingBallnce, settings);

			openPositionOrder = await _tradingDataConnector.CreateOrder(openPositionOrder);

			_orderRepository.Insert(openPositionOrder.ToEntity());
			_orderRepository.Insert(closePositionOrder.ToEntity());
			_orderRepository.Insert(stopLossOrder.ToEntity());

			_loggingService.LogAction(openPositionOrder.ToLogAction(OrderActionType.Create));
			_loggingService.LogAction(closePositionOrder.ToLogAction(OrderActionType.Create));
			_loggingService.LogAction(stopLossOrder.ToLogAction(OrderActionType.Create));
		}

		public async Task UpdateOrder(OrderPair orderPair, TradingSettings settings)
		{
			var storedOrderEntity = GenerateOrderPairs(_orderRepository.GetAll()
					.Where(entity => String.Equals(entity.CurrencyPair, orderPair.OpenPositionOrder.CurrencyPair.Id, StringComparison.OrdinalIgnoreCase))
					.ToList())
				.Single();

			if (storedOrderEntity.Item1.OrderStateType != OrderStateType.Filled)
			{
				var openPositionOrder = await _tradingDataConnector.CancelOrder(orderPair.OpenPositionOrder);
				if (openPositionOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed");

				orderPair.OpenPositionOrder.ClientId = Guid.NewGuid();
				orderPair.ClosePositionOrder.ParentClientId = orderPair.OpenPositionOrder.ClientId;
				orderPair.StopLossOrder.ParentClientId = orderPair.OpenPositionOrder.ClientId;

				var tradingBallnce = (await _tradingDataConnector.GetTradingBallnce())
					.FirstOrDefault(ballance => String.Equals(ballance.CurrencyId, orderPair.OpenPositionOrder.CurrencyPair.QuoteCurrencyId, StringComparison.OrdinalIgnoreCase));

				if (tradingBallnce == null)
					throw new BusinessException("Trading balance is not available");

				orderPair.OpenPositionOrder.CalculateOrderAmount(tradingBallnce, settings);

				orderPair.OpenPositionOrder = await _tradingDataConnector.CreateOrder(orderPair.OpenPositionOrder);

				_orderRepository.Update(orderPair.OpenPositionOrder.ToEntity(storedOrderEntity.Item1));
				_orderRepository.Update(orderPair.ClosePositionOrder.ToEntity(storedOrderEntity.Item2));
				_orderRepository.Update(orderPair.StopLossOrder.ToEntity(storedOrderEntity.Item3));

				_loggingService.LogAction(orderPair.OpenPositionOrder.ToLogAction(OrderActionType.Update));
				_loggingService.LogAction(orderPair.ClosePositionOrder.ToLogAction(OrderActionType.Update));
				_loggingService.LogAction(orderPair.StopLossOrder.ToLogAction(OrderActionType.Update));
			}
			else
			{
				var closePositionOrder = await _tradingDataConnector.CancelOrder(orderPair.ClosePositionOrder);
				if (closePositionOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed");

				var stopLossOrder = await _tradingDataConnector.CancelOrder(orderPair.StopLossOrder);
				if (stopLossOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed");

				orderPair.ClosePositionOrder.ClientId = Guid.NewGuid();
				orderPair.StopLossOrder.ClientId = Guid.NewGuid();

				var tradingBallnce = (await _tradingDataConnector.GetTradingBallnce())
					.FirstOrDefault(ballance => String.Equals(ballance.CurrencyId, orderPair.ClosePositionOrder.CurrencyPair.BaseCurrencyId, StringComparison.OrdinalIgnoreCase));

				if (tradingBallnce == null)
					throw new BusinessException("Trading balance is not available");

				orderPair.ClosePositionOrder.CalculateOrderAmount(tradingBallnce, settings);
				orderPair.StopLossOrder.CalculateOrderAmount(tradingBallnce, settings);

				orderPair.ClosePositionOrder = await _tradingDataConnector.CreateOrder(orderPair.ClosePositionOrder);
				orderPair.StopLossOrder = await _tradingDataConnector.CreateOrder(orderPair.StopLossOrder);

				_orderRepository.Update(orderPair.ClosePositionOrder.ToEntity(storedOrderEntity.Item2));
				_orderRepository.Update(orderPair.StopLossOrder.ToEntity(storedOrderEntity.Item3));
				_loggingService.LogAction(orderPair.ClosePositionOrder.ToLogAction(OrderActionType.Update));
				_loggingService.LogAction(orderPair.StopLossOrder.ToLogAction(OrderActionType.Update));
			}
		}

		public async Task CancelOrder(OrderPair orderPair)
		{
			var storedOrderEntity = GenerateOrderPairs(_orderRepository.GetAll()
					.Where(entity => String.Equals(entity.CurrencyPair, orderPair.OpenPositionOrder.CurrencyPair.Id, StringComparison.OrdinalIgnoreCase))
					.ToList())
				.Single();
			if (storedOrderEntity.Item1.OrderStateType != OrderStateType.Filled)
			{
				orderPair.OpenPositionOrder = await _tradingDataConnector.CancelOrder(orderPair.OpenPositionOrder);
				if (orderPair.OpenPositionOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed");
			}
			else
			{
				orderPair.ClosePositionOrder = await _tradingDataConnector.CancelOrder(orderPair.ClosePositionOrder);
				if (orderPair.ClosePositionOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed");

				var stopLossOrder = await _tradingDataConnector.CancelOrder(orderPair.StopLossOrder);
				if (stopLossOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed");
			}

			_orderHistoryRepository.Insert(orderPair.OpenPositionOrder.ToHistory());
			_orderHistoryRepository.Insert(orderPair.ClosePositionOrder.ToHistory());
			_orderHistoryRepository.Insert(orderPair.StopLossOrder.ToHistory());

			_orderRepository.Delete(storedOrderEntity.Item1);
			_orderRepository.Delete(storedOrderEntity.Item2);
			_orderRepository.Delete(storedOrderEntity.Item3);
			_orderRepository.SaveChanges();

			_loggingService.LogAction(orderPair.OpenPositionOrder.ToLogAction(OrderActionType.Cancel));
			_loggingService.LogAction(orderPair.ClosePositionOrder.ToLogAction(OrderActionType.Cancel));
			_loggingService.LogAction(orderPair.StopLossOrder.ToLogAction(OrderActionType.Cancel));

			_loggingService.LogAction(orderPair.OpenPositionOrder.ToLogAction(OrderActionType.History));
			_loggingService.LogAction(orderPair.ClosePositionOrder.ToLogAction(OrderActionType.History));
			_loggingService.LogAction(orderPair.StopLossOrder.ToLogAction(OrderActionType.History));
		}

		private IList<Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order>> GenerateOrderPairs(IList<Domain.Core.Entities.Trading.Order> orderEntities)
		{
			var result = new List<Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order>>();
			foreach (var openPositionOrderEntity in orderEntities.Where(entity => entity.ClientId == Guid.Empty).ToList())
				result.Add(
					new Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order>(
						openPositionOrderEntity,
						orderEntities.Single(entity => entity.ParentClientId == openPositionOrderEntity.ClientId && entity.OrderType == OrderType.Limit),
						orderEntities.Single(entity => entity.ParentClientId == openPositionOrderEntity.ClientId && entity.OrderType == OrderType.StopMarket)));
			return result;
		}
	}
}