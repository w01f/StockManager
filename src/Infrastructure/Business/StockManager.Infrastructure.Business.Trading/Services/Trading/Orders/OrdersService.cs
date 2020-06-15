﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Services.Extensions.Connectors;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Orders
{
	public class OrdersService : IOrdersService
	{
		private readonly OrderBookLoadingService _orderBookLoadingService;
		private readonly IStockRestConnector _stockRestConnector;
		private readonly ConfigurationService _configurationService;

		public OrdersService(OrderBookLoadingService orderBookLoadingService,
			IStockRestConnector tradingDataConnector,
			ConfigurationService configurationService)
		{
			_orderBookLoadingService = orderBookLoadingService ?? throw new ArgumentNullException(nameof(orderBookLoadingService));
			_stockRestConnector = tradingDataConnector ?? throw new ArgumentNullException(nameof(tradingDataConnector));
			_configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
		}

		public async Task<IList<Order>> GetActiveOrders(Infrastructure.Common.Models.Market.CurrencyPair currencyPair)
		{
			return await _stockRestConnector.GetActiveOrders(currencyPair);
		}

		public async Task<Order> GetOrderFromHistory(Guid clientOrderId, Infrastructure.Common.Models.Market.CurrencyPair currencyPair)
		{
			return await _stockRestConnector.GetOrderFromHistory(clientOrderId, currencyPair);
		}

		public async Task<Order> CreateBuyLimitOrder(Order order)
		{
			var tradingSettings = _configurationService.GetTradingSettings();
			var currencyPair = order.CurrencyPair;
			var quoteTradingBalance = await _stockRestConnector.GetTradingBalance(currencyPair.QuoteCurrencyId);
			if (quoteTradingBalance?.Available <= 0)
				throw new BusinessException($"Trading balance is empty or not available: {currencyPair.Id}");

			var needToResetOrder = order.OrderStateType != OrderStateType.New;

			Order serverSideOrder = null;
			do
			{
				if (serverSideOrder != null)
				{
					if (needToResetOrder)
					{
						order.ClientId  = Guid.NewGuid();
						order.OrderType = OrderType.Limit;
						order.OrderStateType = OrderStateType.New;
						order.StopPrice = null;
					}

					var nearestBidSupportPrice = await _orderBookLoadingService.GetNearestBidSupportPrice(order.CurrencyPair);
					order.Price = nearestBidSupportPrice + order.CurrencyPair.TickSize;
				}

				order.CalculateBuyOrderQuantity(quoteTradingBalance, tradingSettings);
				if (order.Quantity == 0)
					break;

				try
				{
					serverSideOrder = await _stockRestConnector.CreateOrder(order, true);
				}
				catch (Exception)
				{
					serverSideOrder = null;
				}
			} while (serverSideOrder == null || serverSideOrder.OrderStateType == OrderStateType.Expired);

			if (serverSideOrder == null || serverSideOrder.OrderStateType == OrderStateType.Cancelled)
				throw new BusinessException($"Error while creating new position occured: {currencyPair.Id}");

			order.SyncWithAnotherOrder(serverSideOrder);

			return order;
		}

		public async Task<Order> CreateSellLimitOrder(Order order)
		{
			var baseTradingBalance = await _stockRestConnector.GetTradingBalance(order.CurrencyPair.BaseCurrencyId);
			if (baseTradingBalance?.Available <= 0)
				throw new BusinessException($"Trading balance is empty or not available: {order.CurrencyPair.Id}");

			var needToStopPrice = !(order.OrderStateType == OrderStateType.New || order.OrderStateType == OrderStateType.PartiallyFilled);

			Order serverSideOrder = null;
			do
			{
				if (serverSideOrder != null)
				{
					if (needToStopPrice)
					{
						order.OrderType = OrderType.Limit;
						order.OrderStateType = OrderStateType.New;
						order.StopPrice = null;
					}

					var nearestAskSupportPrice = await _orderBookLoadingService.GetNearestAskSupportPrice(order.CurrencyPair);
					order.Price = nearestAskSupportPrice - order.CurrencyPair.TickSize;
				}

				order.CalculateSellOrderQuantity(baseTradingBalance);
				if (order.Quantity == 0)
					throw new BusinessException($"Trading balance is not enough to open order: {order.CurrencyPair.Id}");

				try
				{
					serverSideOrder = await _stockRestConnector.CreateOrder(order, true);
				}
				catch
				{
					serverSideOrder = null;
				}
			} while (serverSideOrder == null || serverSideOrder.OrderStateType == OrderStateType.Expired);

			order.SyncWithAnotherOrder(serverSideOrder);

			return order;
		}

		public async Task<Order> CreateSellMarketOrder(Order order)
		{
			var baseTradingBalance = await _stockRestConnector.GetTradingBalance(order.CurrencyPair.BaseCurrencyId);
			if (baseTradingBalance?.Available <= 0)
				throw new BusinessException($"Trading balance is empty or not available: {order.CurrencyPair.Id}");

			order.CalculateSellOrderQuantity(baseTradingBalance);
			if (order.Quantity == 0)
				throw new BusinessException($"Trading balance is not enough to open order: {order.CurrencyPair.Id}");

			order.ClientId = Guid.NewGuid();
			var serverSideOrder = await _stockRestConnector.CreateOrder(order, false);
			order.SyncWithAnotherOrder(serverSideOrder);

			return order;
		}

		public async Task<Order> CancelOrder(Order order)
		{
			var serverSideOrder = await _stockRestConnector.CancelOrder(order);
			if (serverSideOrder.OrderStateType != OrderStateType.Cancelled)
				throw new BusinessException("Cancelling order failed")
				{
					Details = $"Order: {JsonConvert.SerializeObject(serverSideOrder)}"
				};

			order.SyncWithAnotherOrder(serverSideOrder);
			return order;
		}
	}
}