using Newtonsoft.Json;
using StockManager.Domain.Core.Entities.Trading;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Utilities.Logging.Common.Enums;
using StockManager.Infrastructure.Utilities.Logging.Models.Orders;
using StockManager.Infrastructure.Utilities.Logging.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.Services.Extensions.Connectors;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Common;
using StockManager.Infrastructure.Connectors.Common.Common;


namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Orders
{
	public class OrdersService : IOrdersService
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly IRepository<OrderHistory> _orderHistoryRepository;
		private readonly IMarketDataRestConnector _marketDataRestConnector;
		private readonly ITradingDataRestConnector _tradingDataRestConnector;
		private readonly ConfigurationService _configurationService;
		private readonly ILoggingService _loggingService;
		private readonly TradingEventsObserver _tradingEventsObserver;

		public OrdersService(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
			IRepository<OrderHistory> orderHistoryRepository,
			IMarketDataRestConnector marketDataRestConnector,
			ITradingDataRestConnector tradingDataRestConnector,
			ConfigurationService configurationService,
			ILoggingService loggingService,
			TradingEventsObserver tradingEventsObserver)
		{
			_orderRepository = orderRepository;
			_orderHistoryRepository = orderHistoryRepository;
			_marketDataRestConnector = marketDataRestConnector;
			_tradingDataRestConnector = tradingDataRestConnector;
			_configurationService = configurationService;
			_loggingService = loggingService;
			_tradingEventsObserver = tradingEventsObserver;
		}

		public async Task SyncOrders()
		{
			var storedOrders = _orderRepository.GetAll().ToList();
			if (!storedOrders.Any())
				return;
			var storedOrderPairEntities = storedOrders.GenerateOrderPairs();

			var tradingSettings = _configurationService.GetTradingSettings();

			foreach (var storedOrderEntity in storedOrderPairEntities)
			{
				var currencyPair = await _marketDataRestConnector.GetCurrensyPair(storedOrderEntity.Item1.CurrencyPair);

				var activeOrders = await _tradingDataRestConnector.GetActiveOrders(currencyPair);

				var openPositionOrder = storedOrderEntity.Item1.ToModel(currencyPair);
				if (openPositionOrder.OrderStateType != OrderStateType.Filled)
				{
					var serverSideOpenPositionOrder = activeOrders.FirstOrDefault(order => order.ClientId == storedOrderEntity.Item1.ClientId) ??
						await _tradingDataRestConnector.GetOrderFromHistory(storedOrderEntity.Item1.ClientId, currencyPair);

					if (serverSideOpenPositionOrder == null)
						throw new BusinessException("Open position order not found");

					openPositionOrder.SyncWithAnotherOrder(serverSideOpenPositionOrder);

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
						case OrderStateType.PartiallyFilled:
							await _tradingDataRestConnector.CancelOrder(openPositionOrder);
							openPositionOrder.OrderStateType = OrderStateType.Filled;
							_orderRepository.Update(openPositionOrder.ToEntity(storedOrderEntity.Item1));
							_loggingService.LogAction(openPositionOrder.ToLogAction(OrderActionType.Fill));
							break;
						case OrderStateType.Filled:
							_orderRepository.Update(openPositionOrder.ToEntity(storedOrderEntity.Item1));
							_loggingService.LogAction(openPositionOrder.ToLogAction(OrderActionType.Fill));

							_tradingEventsObserver.RaisePositionChanged(TradingEventType.PositionOpened, $"(Pair: {openPositionOrder.CurrencyPair.Id})");
							break;
						case OrderStateType.Cancelled:
							var closePositionOrder = storedOrderEntity.Item2.ToModel(currencyPair);
							var stopLossOrder = storedOrderEntity.Item3.ToModel(currencyPair);

							_loggingService.LogAction(openPositionOrder.ToLogAction(OrderActionType.Cancel));
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
							throw new BusinessException("Unexpected order state found")
							{
								Details = String.Format("Open posituon order: {0}", JsonConvert.SerializeObject(openPositionOrder))
							};
					}
				}

				if (openPositionOrder.OrderStateType == OrderStateType.Filled)
				{
					var stopLossOrder = storedOrderEntity.Item3.ToModel(currencyPair);
					var serverSideStopLossOrder = activeOrders.FirstOrDefault(order => order.ClientId == storedOrderEntity.Item3.ClientId) ??
						await _tradingDataRestConnector.GetOrderFromHistory(storedOrderEntity.Item3.ClientId, currencyPair);

					if (serverSideStopLossOrder == null)
					{
						var baseTradingBalance = await _tradingDataRestConnector.GetTradingBallnce(stopLossOrder.CurrencyPair.BaseCurrencyId);
						if (baseTradingBalance?.Available <= 0)
							throw new BusinessException(String.Format("Trading balance is empty or not available: {0}", stopLossOrder.CurrencyPair.Id));

						stopLossOrder.CalculateSellOrderQuantity(baseTradingBalance, tradingSettings);
						if (stopLossOrder.Quantity == 0)
							throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", stopLossOrder.CurrencyPair.Id));

						serverSideStopLossOrder = await _tradingDataRestConnector.CreateOrder(stopLossOrder, false);
						stopLossOrder.SyncWithAnotherOrder(serverSideStopLossOrder);

						_orderRepository.Update(stopLossOrder.ToEntity(storedOrderEntity.Item3));
						_loggingService.LogAction(stopLossOrder.ToLogAction(OrderActionType.Update));
					}
					else
						stopLossOrder.SyncWithAnotherOrder(serverSideStopLossOrder);

					var closePositionOrder = storedOrderEntity.Item2.ToModel(currencyPair);
					if (closePositionOrder.OrderStateType != OrderStateType.Pending)
					{
						var serverSideClosePositionOrder = activeOrders.FirstOrDefault(order => order.ClientId == storedOrderEntity.Item2.ClientId) ??
															await _tradingDataRestConnector.GetOrderFromHistory(storedOrderEntity.Item2.ClientId, currencyPair);

						if (serverSideClosePositionOrder == null)
						{
							var baseTradingBalance = await _tradingDataRestConnector.GetTradingBallnce(closePositionOrder.CurrencyPair.BaseCurrencyId);
							if (baseTradingBalance?.Available <= 0)
								throw new BusinessException(String.Format("Trading balance is empty or not available: {0}", closePositionOrder.CurrencyPair.Id));
							do
							{
								if (serverSideClosePositionOrder != null)
								{
									var nearestAskSupportPrice = await _marketDataRestConnector.GetNearestAskSupportPrice(closePositionOrder.CurrencyPair);
									closePositionOrder.Price = nearestAskSupportPrice - closePositionOrder.CurrencyPair.TickSize;
								}

								closePositionOrder.CalculateSellOrderQuantity(baseTradingBalance, tradingSettings);
								if (closePositionOrder.Quantity == 0)
									throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", closePositionOrder.CurrencyPair.Id));

								try
								{
									serverSideClosePositionOrder = await _tradingDataRestConnector.CreateOrder(closePositionOrder, true);
								}
								catch
								{
									serverSideClosePositionOrder = null;
								}
							} while (serverSideClosePositionOrder == null || serverSideClosePositionOrder.OrderStateType == OrderStateType.Expired);
						}

						closePositionOrder.SyncWithAnotherOrder(serverSideClosePositionOrder);

						if (closePositionOrder.OrderStateType == OrderStateType.Filled &&
							stopLossOrder.OrderStateType != OrderStateType.Cancelled)
						{
							try
							{
								serverSideStopLossOrder = await _tradingDataRestConnector.CancelOrder(stopLossOrder);
								stopLossOrder.SyncWithAnotherOrder(serverSideStopLossOrder);
							}
							catch (Exception e)
							{
								stopLossOrder.OrderStateType = OrderStateType.Cancelled;
								Console.WriteLine(e);
							}
						}
						else if (stopLossOrder.OrderStateType == OrderStateType.Filled &&
								closePositionOrder.OrderStateType != OrderStateType.Cancelled)
						{
							serverSideClosePositionOrder = await _tradingDataRestConnector.CancelOrder(closePositionOrder);
							closePositionOrder.SyncWithAnotherOrder(serverSideClosePositionOrder);
						}
						else if (closePositionOrder.OrderStateType != OrderStateType.Filled &&
								stopLossOrder.OrderStateType == OrderStateType.Cancelled)
						{
							if (closePositionOrder.OrderStateType != OrderStateType.Cancelled)
							{
								serverSideClosePositionOrder = await _tradingDataRestConnector.CancelOrder(closePositionOrder);
								closePositionOrder.SyncWithAnotherOrder(serverSideClosePositionOrder);
							}

							var immediateCloseOrder = new Infrastructure.Common.Models.Trading.Order();
							immediateCloseOrder.ClientId = Guid.NewGuid();
							immediateCloseOrder.CurrencyPair = currencyPair;
							immediateCloseOrder.Role = OrderRoleType.ClosePosition;
							immediateCloseOrder.OrderSide = tradingSettings.OppositeOrderSide;
							immediateCloseOrder.OrderType = OrderType.Market;
							immediateCloseOrder.OrderStateType = OrderStateType.New;
							immediateCloseOrder.TimeInForce = OrderTimeInForceType.GoodTillCancelled;

							var baseTradingBalance = await _tradingDataRestConnector.GetTradingBallnce(closePositionOrder.CurrencyPair.BaseCurrencyId);
							immediateCloseOrder.CalculateSellOrderQuantity(baseTradingBalance, tradingSettings);
							if (immediateCloseOrder.Quantity == 0)
								throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", immediateCloseOrder.CurrencyPair.Id));

							var serverSideImmediateCloseOrder = await _tradingDataRestConnector.CreateOrder(immediateCloseOrder, false);
							if (serverSideImmediateCloseOrder.OrderStateType != OrderStateType.Filled)
								throw new BusinessException("Unexpected order state found")
								{
									Details = String.Format("Close position market order: {0}", JsonConvert.SerializeObject(serverSideImmediateCloseOrder))
								};
						}
					}
					else if (stopLossOrder.OrderStateType != OrderStateType.Suspended)
						closePositionOrder.OrderStateType = OrderStateType.Cancelled;

					switch (closePositionOrder.OrderStateType)
					{
						case OrderStateType.Pending:
							break;
						case OrderStateType.Suspended:
						case OrderStateType.New:
						case OrderStateType.PartiallyFilled:
							if (storedOrderEntity.Item2.OrderStateType != closePositionOrder.OrderStateType)
							{
								_orderRepository.Update(closePositionOrder.ToEntity(storedOrderEntity.Item2));
								_loggingService.LogAction(closePositionOrder.ToLogAction(OrderActionType.Update));
							}
							break;
						case OrderStateType.Filled:
							if (stopLossOrder.OrderStateType != OrderStateType.Cancelled)
								throw new BusinessException("Unexpected order state found")
								{
									Details = String.Format("Stop loss order: {0}", JsonConvert.SerializeObject(stopLossOrder))
								};

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

							_tradingEventsObserver.RaisePositionChanged(TradingEventType.PositionClosedSuccessfully, $"(Pair: {closePositionOrder.CurrencyPair.Id})");
							break;
						case OrderStateType.Cancelled:
							if (stopLossOrder.OrderStateType != OrderStateType.Filled && stopLossOrder.OrderStateType != OrderStateType.Cancelled)
								throw new BusinessException("Unexpected order state found")
								{
									Details = String.Format("Stop loss order: {0}", JsonConvert.SerializeObject(stopLossOrder))
								};

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

							_tradingEventsObserver.RaisePositionChanged(TradingEventType.PositionClosedDueStopLoss, $"(Pair: {openPositionOrder.CurrencyPair.Id})");
							break;
						default:
							throw new BusinessException("Unexpected order state found")
							{
								Details = String.Format("Close position order: {0}", JsonConvert.SerializeObject(closePositionOrder))
							};
					}
				}
			}
		}

		public async Task<IList<OrderPair>> GetActiveOrders()
		{
			var storedOrderPairs = _orderRepository.GetAll().ToList().GenerateOrderPairs();

			var orderPairModels = new List<OrderPair>();
			foreach (var orderPair in storedOrderPairs)
			{
				var currencyPair = await _marketDataRestConnector.GetCurrensyPair(orderPair.Item1.CurrencyPair);

				if (currencyPair == null)
					throw new BusinessException("Currency pair not found")
					{
						Details = String.Format("Currency pair id: {0}", orderPair.Item1.CurrencyPair)
					};

				orderPairModels.Add(orderPair.ToModel(currencyPair));
			}

			return orderPairModels;
		}

		public async Task OpenPosition(NewOrderPositionInfo positionInfo)
		{
			var tradingSettings = _configurationService.GetTradingSettings();

			var currencyPair = await _marketDataRestConnector.GetCurrensyPair(positionInfo.CurrencyPairId);

			var now = DateTime.UtcNow;

			var openPositionOrder = new Infrastructure.Common.Models.Trading.Order();
			openPositionOrder.ClientId = Guid.NewGuid();
			openPositionOrder.CurrencyPair = currencyPair;
			openPositionOrder.Role = OrderRoleType.OpenPosition;
			openPositionOrder.OrderSide = tradingSettings.BaseOrderSide;
			openPositionOrder.OrderType = OrderType.StopLimit;
			openPositionOrder.OrderStateType = OrderStateType.Suspended;
			openPositionOrder.TimeInForce = OrderTimeInForceType.GoodTillCancelled;
			openPositionOrder.Price = positionInfo.OpenPrice;
			openPositionOrder.StopPrice = positionInfo.OpenStopPrice;
			openPositionOrder.Created = now;
			openPositionOrder.Updated = now;

			var closePositionOrder = new Infrastructure.Common.Models.Trading.Order();
			closePositionOrder.ClientId = Guid.NewGuid();
			closePositionOrder.ParentClientId = openPositionOrder.ClientId;
			closePositionOrder.CurrencyPair = currencyPair;
			closePositionOrder.Role = OrderRoleType.ClosePosition;
			closePositionOrder.OrderSide = tradingSettings.OppositeOrderSide;
			closePositionOrder.OrderType = OrderType.StopLimit;
			closePositionOrder.OrderStateType = OrderStateType.Pending;
			closePositionOrder.TimeInForce = OrderTimeInForceType.GoodTillCancelled;
			closePositionOrder.Price = positionInfo.ClosePrice;
			closePositionOrder.StopPrice = positionInfo.CloseStopPrice;
			closePositionOrder.Created = now;
			closePositionOrder.Updated = now;

			var stopLossOrder = new Infrastructure.Common.Models.Trading.Order();
			stopLossOrder.ClientId = Guid.NewGuid();
			stopLossOrder.ParentClientId = openPositionOrder.ClientId;
			stopLossOrder.CurrencyPair = currencyPair;
			stopLossOrder.Role = OrderRoleType.StopLoss;
			stopLossOrder.OrderSide = tradingSettings.OppositeOrderSide;
			stopLossOrder.OrderType = OrderType.StopMarket;
			stopLossOrder.OrderStateType = OrderStateType.Suspended;
			stopLossOrder.TimeInForce = OrderTimeInForceType.GoodTillCancelled;
			stopLossOrder.Price = 0;
			stopLossOrder.StopPrice = positionInfo.StopLossPrice;
			stopLossOrder.Created = now;
			stopLossOrder.Updated = now;

			var quoteTradingBalance = await _tradingDataRestConnector.GetTradingBallnce(currencyPair.QuoteCurrencyId);
			if (quoteTradingBalance?.Available <= 0)
				throw new BusinessException(String.Format("Trading balance is empty or not available: {0}", currencyPair.Id));

			Infrastructure.Common.Models.Trading.Order serverSideOpenPositionOrder = null;
			do
			{
				if (serverSideOpenPositionOrder != null)
				{
					openPositionOrder.OrderType = OrderType.Limit;
					openPositionOrder.OrderStateType = OrderStateType.New;
					openPositionOrder.StopPrice = null;

					var nearestBidSupportPrice = await _marketDataRestConnector.GetNearestBidSupportPrice(openPositionOrder.CurrencyPair);
					openPositionOrder.Price = nearestBidSupportPrice + openPositionOrder.CurrencyPair.TickSize;
				}

				openPositionOrder.CalculateBuyOrderQuantity(quoteTradingBalance, tradingSettings);
				if (openPositionOrder.Quantity == 0)
					break;

				try
				{
					serverSideOpenPositionOrder = await _tradingDataRestConnector.CreateOrder(openPositionOrder, true);
				}
				catch (Exception)
				{
					serverSideOpenPositionOrder = null;
				}
			} while (serverSideOpenPositionOrder == null || serverSideOpenPositionOrder.OrderStateType == OrderStateType.Expired);

			if (serverSideOpenPositionOrder != null)
			{
				openPositionOrder.SyncWithAnotherOrder(serverSideOpenPositionOrder);

				_orderRepository.Insert(openPositionOrder.ToEntity());
				_orderRepository.Insert(closePositionOrder.ToEntity());
				_orderRepository.Insert(stopLossOrder.ToEntity());

				_loggingService.LogAction(openPositionOrder.ToLogAction(OrderActionType.Create));
				_loggingService.LogAction(closePositionOrder.ToLogAction(OrderActionType.Create));
				_loggingService.LogAction(stopLossOrder.ToLogAction(OrderActionType.Create));

				_tradingEventsObserver.RaisePositionChanged(TradingEventType.NewPosition, $"(Pair: {openPositionOrder.CurrencyPair.Id})");
			}
		}

		public async Task UpdatePosition(OrderPair orderPair)
		{
			var tradingSettings = _configurationService.GetTradingSettings();

			var storedOrderEntity = _orderRepository.GetAll()
					.Where(entity => String.Equals(entity.CurrencyPair, orderPair.OpenPositionOrder.CurrencyPair.Id, StringComparison.OrdinalIgnoreCase))
					.ToList()
					.GenerateOrderPairs()
					.Single();

			if (storedOrderEntity.Item1.OrderStateType != OrderStateType.Filled)
			{
				var serverSideOpenPositionOrder = await _tradingDataRestConnector.CancelOrder(orderPair.OpenPositionOrder);
				if (serverSideOpenPositionOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed")
					{
						Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideOpenPositionOrder))
					};

				orderPair.OpenPositionOrder.ClientId = Guid.NewGuid();
				orderPair.ClosePositionOrder.ParentClientId = orderPair.OpenPositionOrder.ClientId;
				orderPair.StopLossOrder.ParentClientId = orderPair.OpenPositionOrder.ClientId;

				var quoteTradingBalance = await _tradingDataRestConnector.GetTradingBallnce(orderPair.OpenPositionOrder.CurrencyPair.QuoteCurrencyId);
				if (quoteTradingBalance?.Available <= 0)
					throw new BusinessException(String.Format("Trading balance is empty or not available: {0}", orderPair.OpenPositionOrder.CurrencyPair.Id));

				if (orderPair.OpenPositionOrder.OrderStateType == OrderStateType.New)
				{
					serverSideOpenPositionOrder = null;
					do
					{
						if (serverSideOpenPositionOrder != null)
						{
							var nearestBidSupportPrice = await _marketDataRestConnector.GetNearestBidSupportPrice(orderPair.OpenPositionOrder.CurrencyPair);
							orderPair.OpenPositionOrder.Price = nearestBidSupportPrice + orderPair.OpenPositionOrder.CurrencyPair.TickSize;
						}

						orderPair.OpenPositionOrder.CalculateBuyOrderQuantity(quoteTradingBalance, tradingSettings);
						if (orderPair.OpenPositionOrder.Quantity == 0)
							throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", orderPair.OpenPositionOrder.CurrencyPair.Id));

						try
						{
							serverSideOpenPositionOrder = await _tradingDataRestConnector.CreateOrder(orderPair.OpenPositionOrder, true);
						}
						catch
						{
							serverSideOpenPositionOrder = null;
						}
					} while (serverSideOpenPositionOrder == null || serverSideOpenPositionOrder.OrderStateType == OrderStateType.Expired);
				}
				else
				{
					if (orderPair.OpenPositionOrder.StopPrice == null)
						throw new BusinessException("Unexpected order state found")
						{
							Details = String.Format("Open position order: {0}", JsonConvert.SerializeObject(orderPair.OpenPositionOrder))
						};

					serverSideOpenPositionOrder = null;
					do
					{
						if (serverSideOpenPositionOrder != null)
						{
							orderPair.OpenPositionOrder.OrderType = OrderType.Limit;
							orderPair.OpenPositionOrder.OrderStateType = OrderStateType.New;
							orderPair.OpenPositionOrder.StopPrice = null;

							var nearestBidSupportPrice = await _marketDataRestConnector.GetNearestBidSupportPrice(orderPair.OpenPositionOrder.CurrencyPair);
							orderPair.OpenPositionOrder.Price = nearestBidSupportPrice + orderPair.OpenPositionOrder.CurrencyPair.TickSize;
						}

						orderPair.OpenPositionOrder.CalculateBuyOrderQuantity(quoteTradingBalance, tradingSettings);
						if (orderPair.OpenPositionOrder.Quantity == 0)
							throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", orderPair.OpenPositionOrder.CurrencyPair.Id));

						try
						{
							serverSideOpenPositionOrder = await _tradingDataRestConnector.CreateOrder(orderPair.OpenPositionOrder, true);
						}
						catch
						{
							serverSideOpenPositionOrder = null;
						}
					} while (serverSideOpenPositionOrder == null || serverSideOpenPositionOrder.OrderStateType == OrderStateType.Expired);
				}
				orderPair.OpenPositionOrder.SyncWithAnotherOrder(serverSideOpenPositionOrder);

				_orderRepository.Update(orderPair.OpenPositionOrder.ToEntity(storedOrderEntity.Item1));
				_orderRepository.Update(orderPair.ClosePositionOrder.ToEntity(storedOrderEntity.Item2));
				_orderRepository.Update(orderPair.StopLossOrder.ToEntity(storedOrderEntity.Item3));

				_loggingService.LogAction(orderPair.OpenPositionOrder.ToLogAction(OrderActionType.Update));
				_loggingService.LogAction(orderPair.ClosePositionOrder.ToLogAction(OrderActionType.Update));
				_loggingService.LogAction(orderPair.StopLossOrder.ToLogAction(OrderActionType.Update));
			}
			else
			{
				if (orderPair.ClosePositionOrder.OrderStateType != OrderStateType.Pending)
				{
					Infrastructure.Common.Models.Trading.Order serverSideClosePositionOrder;
					if (storedOrderEntity.Item2.OrderStateType != OrderStateType.Pending)
					{
						serverSideClosePositionOrder = await _tradingDataRestConnector.CancelOrder(orderPair.ClosePositionOrder);
						if (serverSideClosePositionOrder.OrderStateType != OrderStateType.Cancelled)
							throw new BusinessException("Cancelling order failed")
							{
								Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideClosePositionOrder))
							};
					}

					var baseTradingBalance = await _tradingDataRestConnector.GetTradingBallnce(orderPair.ClosePositionOrder.CurrencyPair.BaseCurrencyId);
					if (baseTradingBalance?.Available <= 0)
						throw new BusinessException(String.Format("Trading balance is empty or not available: {0}", orderPair.ClosePositionOrder.CurrencyPair.Id));

					orderPair.ClosePositionOrder.ClientId = Guid.NewGuid();

					if (orderPair.ClosePositionOrder.OrderStateType == OrderStateType.New ||
						orderPair.ClosePositionOrder.OrderStateType == OrderStateType.PartiallyFilled)
					{
						serverSideClosePositionOrder = null;
						do
						{
							if (serverSideClosePositionOrder != null)
							{
								var nearestAskSupportPrice = await _marketDataRestConnector.GetNearestAskSupportPrice(orderPair.ClosePositionOrder.CurrencyPair);
								orderPair.ClosePositionOrder.Price = nearestAskSupportPrice - orderPair.ClosePositionOrder.CurrencyPair.TickSize;
							}

							orderPair.ClosePositionOrder.CalculateSellOrderQuantity(baseTradingBalance, tradingSettings);
							if (orderPair.ClosePositionOrder.Quantity == 0)
								throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", orderPair.ClosePositionOrder.CurrencyPair.Id));

							try
							{
								serverSideClosePositionOrder = await _tradingDataRestConnector.CreateOrder(orderPair.ClosePositionOrder, true);
							}
							catch
							{
								serverSideClosePositionOrder = null;
							}
						} while (serverSideClosePositionOrder == null || serverSideClosePositionOrder.OrderStateType == OrderStateType.Expired);
					}
					else
					{
						serverSideClosePositionOrder = null;
						do
						{
							if (serverSideClosePositionOrder != null)
							{
								orderPair.ClosePositionOrder.OrderType = OrderType.Limit;
								orderPair.ClosePositionOrder.OrderStateType = OrderStateType.New;
								orderPair.ClosePositionOrder.StopPrice = null;

								var nearestAskSupportPrice = await _marketDataRestConnector.GetNearestAskSupportPrice(orderPair.ClosePositionOrder.CurrencyPair);
								orderPair.ClosePositionOrder.Price = nearestAskSupportPrice - orderPair.ClosePositionOrder.CurrencyPair.TickSize;
							}

							orderPair.ClosePositionOrder.CalculateSellOrderQuantity(baseTradingBalance, tradingSettings);
							if (orderPair.ClosePositionOrder.Quantity == 0)
								throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", orderPair.ClosePositionOrder.CurrencyPair.Id));

							try
							{
								serverSideClosePositionOrder = await _tradingDataRestConnector.CreateOrder(orderPair.ClosePositionOrder, true);
							}
							catch
							{
								serverSideClosePositionOrder = null;
							}
						} while (serverSideClosePositionOrder == null || serverSideClosePositionOrder.OrderStateType == OrderStateType.Expired);
					}
					orderPair.ClosePositionOrder.SyncWithAnotherOrder(serverSideClosePositionOrder);

					_orderRepository.Update(orderPair.ClosePositionOrder.ToEntity(storedOrderEntity.Item2));
					_loggingService.LogAction(orderPair.ClosePositionOrder.ToLogAction(OrderActionType.Update));
				}
				else
				{
					var serverSideStopLossOrder = await _tradingDataRestConnector.CancelOrder(orderPair.StopLossOrder);
					if (serverSideStopLossOrder.OrderStateType != OrderStateType.Cancelled)
						throw new BusinessException("Cancelling order failed")
						{
							Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideStopLossOrder))
						};

					var baseTradingBalance = await _tradingDataRestConnector.GetTradingBallnce(orderPair.StopLossOrder.CurrencyPair.BaseCurrencyId);
					if (baseTradingBalance?.Available <= 0)
						throw new BusinessException(String.Format("Trading balance is empty or not available: {0}", orderPair.StopLossOrder.CurrencyPair.Id));

					orderPair.StopLossOrder.CalculateSellOrderQuantity(baseTradingBalance, tradingSettings);
					if (orderPair.StopLossOrder.Quantity == 0)
						throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", orderPair.StopLossOrder.CurrencyPair.Id));

					orderPair.StopLossOrder.ClientId = Guid.NewGuid();
					serverSideStopLossOrder = await _tradingDataRestConnector.CreateOrder(orderPair.StopLossOrder, false);
					orderPair.StopLossOrder.SyncWithAnotherOrder(serverSideStopLossOrder);

					_orderRepository.Update(orderPair.StopLossOrder.ToEntity(storedOrderEntity.Item3));
					_loggingService.LogAction(orderPair.StopLossOrder.ToLogAction(OrderActionType.Update));
				}
			}
		}

		public async Task SuspendPosition(OrderPair orderPair)
		{
			var storedOrderEntity = _orderRepository.GetAll()
					.Where(entity => String.Equals(entity.CurrencyPair, orderPair.OpenPositionOrder.CurrencyPair.Id, StringComparison.OrdinalIgnoreCase))
					.ToList()
					.GenerateOrderPairs()
					.Single();

			if (orderPair.OpenPositionOrder.OrderStateType != OrderStateType.Filled)
				return;

			if (orderPair.ClosePositionOrder.OrderStateType == OrderStateType.Suspended)
				return;

			try
			{
				var serverSideClosePositionOrder = await _tradingDataRestConnector.CancelOrder(orderPair.ClosePositionOrder);
				if (serverSideClosePositionOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed")
					{
						Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideClosePositionOrder))
					};

				orderPair.ClosePositionOrder.OrderStateType = OrderStateType.Pending;
				_orderRepository.Update(orderPair.ClosePositionOrder.ToEntity(storedOrderEntity.Item2));
				_loggingService.LogAction(orderPair.ClosePositionOrder.ToLogAction(OrderActionType.Suspend));
			}
			catch
			{
				// ignored
			}
		}

		public async Task CancelPosition(OrderPair orderPair)
		{
			var storedOrderEntity = _orderRepository.GetAll()
					.Where(entity => String.Equals(entity.CurrencyPair, orderPair.OpenPositionOrder.CurrencyPair.Id, StringComparison.OrdinalIgnoreCase))
					.ToList()
					.GenerateOrderPairs()
					.Single();
			var cancellationInterrupted = false;

			if (storedOrderEntity.Item1.OrderStateType != OrderStateType.Filled)
			{
				try
				{
					var serverSideOpenPositionOrder = await _tradingDataRestConnector.CancelOrder(orderPair.OpenPositionOrder);
					if (serverSideOpenPositionOrder.OrderStateType != OrderStateType.Cancelled)
						throw new BusinessException("Cancelling order failed")
						{
							Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideOpenPositionOrder))
						};

					orderPair.OpenPositionOrder.SyncWithAnotherOrder(serverSideOpenPositionOrder);
				}
				catch (ConnectorException e)
				{
					if (e.Message?.Contains("Order not found") ?? false)
					{
						var activeOrders = await _tradingDataRestConnector.GetActiveOrders(orderPair.OpenPositionOrder.CurrencyPair);
						var serverSideOpenPositionOrder = activeOrders.FirstOrDefault(order => order.ClientId == storedOrderEntity.Item1.ClientId) ??
								await _tradingDataRestConnector.GetOrderFromHistory(storedOrderEntity.Item1.ClientId, orderPair.OpenPositionOrder.CurrencyPair);
						orderPair.OpenPositionOrder.SyncWithAnotherOrder(serverSideOpenPositionOrder);
						cancellationInterrupted = true;
					}
					else
						throw;
				}
			}
			else
			{
				var serverSideClosePositionOrder = await _tradingDataRestConnector.CancelOrder(orderPair.ClosePositionOrder);
				if (serverSideClosePositionOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed")
					{
						Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideClosePositionOrder))
					};
				orderPair.ClosePositionOrder.SyncWithAnotherOrder(serverSideClosePositionOrder);

				var serverSideStopLossOrder = await _tradingDataRestConnector.CancelOrder(orderPair.StopLossOrder);
				if (serverSideStopLossOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed")
					{
						Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideStopLossOrder))
					};
				orderPair.StopLossOrder.SyncWithAnotherOrder(serverSideStopLossOrder);
			}

			if (!cancellationInterrupted)
			{
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
		}
	}
}