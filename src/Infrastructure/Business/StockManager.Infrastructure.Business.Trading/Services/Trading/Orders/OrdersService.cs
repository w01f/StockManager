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
using StockManager.Infrastructure.Common.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;


namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Orders
{
	public class OrdersService : IOrdersService
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly IRepository<OrderHistory> _orderHistoryRepository;
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly ITradingDataConnector _tradingDataConnector;
		private readonly ConfigurationService _configurationService;
		private readonly ILoggingService _loggingService;

		public OrdersService(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
			IRepository<OrderHistory> orderHistoryRepository,
			IMarketDataConnector marketDataConnector,
			ITradingDataConnector tradingDataConnector,
			ConfigurationService configurationService,
			ILoggingService loggingService)
		{
			_orderRepository = orderRepository;
			_orderHistoryRepository = orderHistoryRepository;
			_marketDataConnector = marketDataConnector;
			_tradingDataConnector = tradingDataConnector;
			_configurationService = configurationService;
			_loggingService = loggingService;
		}

		public async Task SyncOrders()
		{
			var storedOrders = _orderRepository.GetAll().ToList();
			if (!storedOrders.Any())
				return;
			var storedOrderPairEntities = GenerateOrderPairs(storedOrders);

			var tradingSettings = _configurationService.GetTradingSettings();

			foreach (var storedOrderEntity in storedOrderPairEntities)
			{
				var currencyPair = await _marketDataConnector.GetCurrensyPair(storedOrderEntity.Item1.CurrencyPair);

				var activeOrders = await _tradingDataConnector.GetActiveOrders(currencyPair);

				var openPositionOrder = storedOrderEntity.Item1.ToModel(currencyPair);
				if (openPositionOrder.OrderStateType != OrderStateType.Filled)
				{
					var serverSideOpenPositionOrder = activeOrders.FirstOrDefault(order => order.ClientId == storedOrderEntity.Item1.ClientId) ??
						await _tradingDataConnector.GetOrderFromHistory(storedOrderEntity.Item1.ClientId, currencyPair);

					if (serverSideOpenPositionOrder == null)
						throw new BusinessException("Open position order not found");

					SyncOrderSettingsWithServer(openPositionOrder, serverSideOpenPositionOrder);

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
							await _tradingDataConnector.CancelOrder(openPositionOrder);
							openPositionOrder.OrderStateType = OrderStateType.Filled;
							_orderRepository.Update(openPositionOrder.ToEntity(storedOrderEntity.Item1));
							_loggingService.LogAction(openPositionOrder.ToLogAction(OrderActionType.Fill));
							break;
						case OrderStateType.Filled:
							_orderRepository.Update(openPositionOrder.ToEntity(storedOrderEntity.Item1));
							_loggingService.LogAction(openPositionOrder.ToLogAction(OrderActionType.Fill));
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
						await _tradingDataConnector.GetOrderFromHistory(storedOrderEntity.Item3.ClientId, currencyPair);

					if (serverSideStopLossOrder == null)
					{
						var baseTradingBalance = await _tradingDataConnector.GetTradingBallnce(stopLossOrder.CurrencyPair.BaseCurrencyId);
						if (baseTradingBalance?.Available <= 0)
							throw new BusinessException(String.Format("Trading balance is empty or not available: {0}", stopLossOrder.CurrencyPair.Id));

						stopLossOrder.CalculateSellOrderQuantity(baseTradingBalance, tradingSettings);
						if (stopLossOrder.Quantity == 0)
							throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", stopLossOrder.CurrencyPair.Id));

						serverSideStopLossOrder = await _tradingDataConnector.CreateOrder(stopLossOrder, false);
						SyncOrderSettingsWithServer(stopLossOrder, serverSideStopLossOrder);

						_orderRepository.Update(stopLossOrder.ToEntity(storedOrderEntity.Item3));
						_loggingService.LogAction(stopLossOrder.ToLogAction(OrderActionType.Update));
					}
					else
						SyncOrderSettingsWithServer(stopLossOrder, serverSideStopLossOrder);

					var closePositionOrder = storedOrderEntity.Item2.ToModel(currencyPair);
					if (closePositionOrder.OrderStateType != OrderStateType.Pending)
					{
						var serverSideClosePositionOrder = activeOrders.FirstOrDefault(order => order.ClientId == storedOrderEntity.Item2.ClientId) ??
															await _tradingDataConnector.GetOrderFromHistory(storedOrderEntity.Item2.ClientId, currencyPair);

						SyncOrderSettingsWithServer(closePositionOrder, serverSideClosePositionOrder);

						if (closePositionOrder.OrderStateType == OrderStateType.Filled &&
							stopLossOrder.OrderStateType != OrderStateType.Cancelled)
						{
							try
							{
								serverSideStopLossOrder = await _tradingDataConnector.CancelOrder(stopLossOrder);
								SyncOrderSettingsWithServer(stopLossOrder, serverSideStopLossOrder);
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
							serverSideClosePositionOrder = await _tradingDataConnector.CancelOrder(closePositionOrder);
							SyncOrderSettingsWithServer(closePositionOrder, serverSideClosePositionOrder);
						}
						else if (closePositionOrder.OrderStateType != OrderStateType.Filled &&
								stopLossOrder.OrderStateType == OrderStateType.Cancelled)
						{
							if (closePositionOrder.OrderStateType != OrderStateType.Cancelled)
							{
								serverSideClosePositionOrder = await _tradingDataConnector.CancelOrder(closePositionOrder);
								SyncOrderSettingsWithServer(closePositionOrder, serverSideClosePositionOrder);
							}

							var immediateCloseOrder = new Common.Models.Trading.Order();
							immediateCloseOrder.ClientId = Guid.NewGuid();
							immediateCloseOrder.CurrencyPair = currencyPair;
							immediateCloseOrder.Role = OrderRoleType.ClosePosition;
							immediateCloseOrder.OrderSide = tradingSettings.OppositeOrderSide;
							immediateCloseOrder.OrderType = OrderType.Market;
							immediateCloseOrder.OrderStateType = OrderStateType.New;
							immediateCloseOrder.TimeInForce = OrderTimeInForceType.GoodTillCancelled;

							var baseTradingBalance = await _tradingDataConnector.GetTradingBallnce(closePositionOrder.CurrencyPair.BaseCurrencyId);
							immediateCloseOrder.CalculateSellOrderQuantity(baseTradingBalance, tradingSettings);
							if (immediateCloseOrder.Quantity == 0)
								throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", immediateCloseOrder.CurrencyPair.Id));

							var serverSideImmediateCloseOrder = await _tradingDataConnector.CreateOrder(immediateCloseOrder, false);
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
			var storedOrderPairs = GenerateOrderPairs(_orderRepository.GetAll()
				.ToList());

			var orderPairModels = new List<OrderPair>();
			foreach (var orderPair in storedOrderPairs)
			{
				var currencyPair = await _marketDataConnector.GetCurrensyPair(orderPair.Item1.CurrencyPair);

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

			var currencyPair = await _marketDataConnector.GetCurrensyPair(positionInfo.CurrencyPairId);

			var now = DateTime.UtcNow;

			var openPositionOrder = new Common.Models.Trading.Order();
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

			var closePositionOrder = new Common.Models.Trading.Order();
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

			var stopLossOrder = new Common.Models.Trading.Order();
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

			var quoteTradingBalance = await _tradingDataConnector.GetTradingBallnce(currencyPair.QuoteCurrencyId);
			if (quoteTradingBalance?.Available <= 0)
				throw new BusinessException(String.Format("Trading balance is empty or not available: {0}", currencyPair.Id));

			openPositionOrder.CalculateBuyOrderQuantity(quoteTradingBalance, tradingSettings);

			if (openPositionOrder.Quantity == 0)
				throw new BusinessException(String.Format("Trading balance is not enoug to open order: {0}", currencyPair.Id));

			var serverSideOpenPositionOrder = await _tradingDataConnector.CreateOrder(openPositionOrder, false);
			SyncOrderSettingsWithServer(openPositionOrder, serverSideOpenPositionOrder);

			_orderRepository.Insert(openPositionOrder.ToEntity());
			_orderRepository.Insert(closePositionOrder.ToEntity());
			_orderRepository.Insert(stopLossOrder.ToEntity());

			_loggingService.LogAction(openPositionOrder.ToLogAction(OrderActionType.Create));
			_loggingService.LogAction(closePositionOrder.ToLogAction(OrderActionType.Create));
			_loggingService.LogAction(stopLossOrder.ToLogAction(OrderActionType.Create));
		}

		public async Task UpdatePosition(OrderPair orderPair)
		{
			var tradingSettings = _configurationService.GetTradingSettings();

			var storedOrderEntity = GenerateOrderPairs(_orderRepository.GetAll()
					.Where(entity => String.Equals(entity.CurrencyPair, orderPair.OpenPositionOrder.CurrencyPair.Id, StringComparison.OrdinalIgnoreCase))
					.ToList())
				.Single();

			if (storedOrderEntity.Item1.OrderStateType != OrderStateType.Filled)
			{
				var serverSideOpenPositionOrder = await _tradingDataConnector.CancelOrder(orderPair.OpenPositionOrder);
				if (serverSideOpenPositionOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed")
					{
						Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideOpenPositionOrder))
					};

				orderPair.OpenPositionOrder.ClientId = Guid.NewGuid();
				orderPair.ClosePositionOrder.ParentClientId = orderPair.OpenPositionOrder.ClientId;
				orderPair.StopLossOrder.ParentClientId = orderPair.OpenPositionOrder.ClientId;

				var quoteTradingBalance = await _tradingDataConnector.GetTradingBallnce(orderPair.OpenPositionOrder.CurrencyPair.QuoteCurrencyId);
				if (quoteTradingBalance?.Available <= 0)
					throw new BusinessException(String.Format("Trading balance is empty or not available: {0}", orderPair.OpenPositionOrder.CurrencyPair.Id));

				if (orderPair.OpenPositionOrder.OrderStateType == OrderStateType.New)
				{
					serverSideOpenPositionOrder = null;
					do
					{
						if (serverSideOpenPositionOrder != null)
						{
							var nearestBidSupportPrice = await GetNearestBidSupportPrice(orderPair.OpenPositionOrder.CurrencyPair);
							orderPair.OpenPositionOrder.Price = nearestBidSupportPrice + orderPair.OpenPositionOrder.CurrencyPair.TickSize;
						}

						orderPair.OpenPositionOrder.CalculateBuyOrderQuantity(quoteTradingBalance, tradingSettings);
						if (orderPair.OpenPositionOrder.Quantity == 0)
							throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", orderPair.OpenPositionOrder.CurrencyPair.Id));

						serverSideOpenPositionOrder = await _tradingDataConnector.CreateOrder(orderPair.OpenPositionOrder, true);
					} while (serverSideOpenPositionOrder.OrderStateType == OrderStateType.Expired);
				}
				else
				{
					orderPair.OpenPositionOrder.CalculateBuyOrderQuantity(quoteTradingBalance, tradingSettings);
					if (orderPair.OpenPositionOrder.Quantity == 0)
						throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", orderPair.OpenPositionOrder.CurrencyPair.Id));

					serverSideOpenPositionOrder = await _tradingDataConnector.CreateOrder(orderPair.OpenPositionOrder, false);
				}
				SyncOrderSettingsWithServer(orderPair.OpenPositionOrder, serverSideOpenPositionOrder);

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
					Common.Models.Trading.Order serverSideClosePositionOrder;
					if (storedOrderEntity.Item2.OrderStateType != OrderStateType.Pending)
					{
						serverSideClosePositionOrder = await _tradingDataConnector.CancelOrder(orderPair.ClosePositionOrder);
						if (serverSideClosePositionOrder.OrderStateType != OrderStateType.Cancelled)
							throw new BusinessException("Cancelling order failed")
							{
								Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideClosePositionOrder))
							};
					}

					var baseTradingBalance = await _tradingDataConnector.GetTradingBallnce(orderPair.ClosePositionOrder.CurrencyPair.BaseCurrencyId);
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
								var nearestAskSupportPrice = await GetNearestAskSupportPrice(orderPair.OpenPositionOrder.CurrencyPair);
								orderPair.ClosePositionOrder.Price = nearestAskSupportPrice - orderPair.ClosePositionOrder.CurrencyPair.TickSize;
							}

							orderPair.ClosePositionOrder.CalculateSellOrderQuantity(baseTradingBalance, tradingSettings);
							if (orderPair.ClosePositionOrder.Quantity == 0)
								throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", orderPair.ClosePositionOrder.CurrencyPair.Id));

							serverSideClosePositionOrder = await _tradingDataConnector.CreateOrder(orderPair.OpenPositionOrder, true);
						} while (serverSideClosePositionOrder.OrderStateType == OrderStateType.Expired);
					}
					else
					{
						orderPair.ClosePositionOrder.CalculateSellOrderQuantity(baseTradingBalance, tradingSettings);
						if (orderPair.ClosePositionOrder.Quantity == 0)
							throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", orderPair.ClosePositionOrder.CurrencyPair.Id));

						serverSideClosePositionOrder = await _tradingDataConnector.CreateOrder(orderPair.ClosePositionOrder, false);
					}
					SyncOrderSettingsWithServer(orderPair.ClosePositionOrder, serverSideClosePositionOrder);

					_orderRepository.Update(orderPair.ClosePositionOrder.ToEntity(storedOrderEntity.Item2));
					_loggingService.LogAction(orderPair.ClosePositionOrder.ToLogAction(OrderActionType.Update));
				}
				else
				{
					var serverSideStopLossOrder = await _tradingDataConnector.CancelOrder(orderPair.StopLossOrder);
					if (serverSideStopLossOrder.OrderStateType != OrderStateType.Cancelled)
						throw new BusinessException("Cancelling order failed")
						{
							Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideStopLossOrder))
						};

					var baseTradingBalance = await _tradingDataConnector.GetTradingBallnce(orderPair.StopLossOrder.CurrencyPair.BaseCurrencyId);
					if (baseTradingBalance?.Available <= 0)
						throw new BusinessException(String.Format("Trading balance is empty or not available: {0}", orderPair.StopLossOrder.CurrencyPair.Id));

					orderPair.StopLossOrder.CalculateSellOrderQuantity(baseTradingBalance, tradingSettings);
					if (orderPair.StopLossOrder.Quantity == 0)
						throw new BusinessException(String.Format("Trading balance is not enough to open order: {0}", orderPair.StopLossOrder.CurrencyPair.Id));

					orderPair.StopLossOrder.ClientId = Guid.NewGuid();
					serverSideStopLossOrder = await _tradingDataConnector.CreateOrder(orderPair.StopLossOrder, false);
					SyncOrderSettingsWithServer(orderPair.StopLossOrder, serverSideStopLossOrder);

					_orderRepository.Update(orderPair.StopLossOrder.ToEntity(storedOrderEntity.Item3));
					_loggingService.LogAction(orderPair.StopLossOrder.ToLogAction(OrderActionType.Update));
				}
			}
		}

		public async Task CancelPosition(OrderPair orderPair)
		{
			var storedOrderEntity = GenerateOrderPairs(_orderRepository.GetAll()
					.Where(entity => String.Equals(entity.CurrencyPair, orderPair.OpenPositionOrder.CurrencyPair.Id, StringComparison.OrdinalIgnoreCase))
					.ToList())
				.Single();
			if (storedOrderEntity.Item1.OrderStateType != OrderStateType.Filled)
			{
				var serverSideOpenPositionOrder = await _tradingDataConnector.CancelOrder(orderPair.OpenPositionOrder);
				if (serverSideOpenPositionOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed")
					{
						Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideOpenPositionOrder))
					};

				SyncOrderSettingsWithServer(orderPair.OpenPositionOrder, serverSideOpenPositionOrder);
			}
			else
			{
				var serverSideClosePositionOrder = await _tradingDataConnector.CancelOrder(orderPair.ClosePositionOrder);
				if (serverSideClosePositionOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed")
					{
						Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideClosePositionOrder))
					};
				SyncOrderSettingsWithServer(orderPair.ClosePositionOrder, serverSideClosePositionOrder);

				var serverSideStopLossOrder = await _tradingDataConnector.CancelOrder(orderPair.StopLossOrder);
				if (serverSideStopLossOrder.OrderStateType != OrderStateType.Cancelled)
					throw new BusinessException("Cancelling order failed")
					{
						Details = String.Format("Order: {0}", JsonConvert.SerializeObject(serverSideStopLossOrder))
					};
				SyncOrderSettingsWithServer(orderPair.StopLossOrder, serverSideStopLossOrder);
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

		private void SyncOrderSettingsWithServer(
			Common.Models.Trading.Order localOrder,
			Common.Models.Trading.Order serverOrder)
		{
			localOrder.ExtId = serverOrder.ExtId;

			localOrder.OrderSide = serverOrder.OrderSide;
			localOrder.OrderType = serverOrder.OrderType;
			localOrder.OrderStateType = serverOrder.OrderStateType;
			localOrder.TimeInForce = serverOrder.TimeInForce;
		}

		private IList<Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order>> GenerateOrderPairs(IList<Domain.Core.Entities.Trading.Order> orderEntities)
		{
			var result = new List<Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order>>();
			foreach (var openPositionOrderEntity in orderEntities.Where(entity => entity.Role == OrderRoleType.OpenPosition)
				.ToList())
			{
				result.Add(
					new Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order,
						Domain.Core.Entities.Trading.Order>(
						openPositionOrderEntity,
						orderEntities.Single(entity =>
							entity.ParentClientId == openPositionOrderEntity.ClientId && entity.Role == OrderRoleType.ClosePosition),
						orderEntities.Single(entity =>
							entity.ParentClientId == openPositionOrderEntity.ClientId && entity.Role == OrderRoleType.StopLoss)));
			}
			return result;
		}

		private async Task<decimal> GetNearestBidSupportPrice(CurrencyPair currencyPair)
		{
			var orderBookBidItems = (await _marketDataConnector.GetOrderBook(currencyPair.Id, 20))
				.Where(item => item.Type == OrderBookItemType.Bid)
				.ToList();

			if (!orderBookBidItems.Any())
				throw new NoNullAllowedException("Couldn't load order book");

			var avgBidSize = orderBookBidItems
				.Average(item => item.Size);

			var topBidPrice = orderBookBidItems
				.OrderByDescending(item => item.Price)
				.Select(item => item.Price)
				.First();

			return orderBookBidItems
				.Where(item => item.Size > avgBidSize && item.Price < topBidPrice)
				.OrderByDescending(item => item.Price)
				.Select(item => item.Price)
				.First();
		}

		private async Task<decimal> GetNearestAskSupportPrice(CurrencyPair currencyPair)
		{
			var orderBookAskItems = (await _marketDataConnector.GetOrderBook(currencyPair.Id, 20))
				.Where(item => item.Type == OrderBookItemType.Ask)
				.ToList();

			if (!orderBookAskItems.Any())
				throw new NoNullAllowedException("Couldn't load order book");

			var avgAskSize = orderBookAskItems
				.Average(item => item.Size);

			var bottomAskPrice = orderBookAskItems
				.OrderBy(item => item.Price)
				.Select(item => item.Price)
				.First();

			return orderBookAskItems
				.Where(item => item.Size > avgAskSize && item.Price > bottomAskPrice)
				.OrderBy(item => item.Price)
				.Select(item => item.Price)
				.First();
		}
	}
}