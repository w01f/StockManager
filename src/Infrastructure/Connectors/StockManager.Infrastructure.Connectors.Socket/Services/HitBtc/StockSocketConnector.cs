using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Enums;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Socket.Connection.HitBtc;
using StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.Market;
using StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.NotificationParameters;
using StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.RequestParameters;
using StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.Trading;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Connectors.Socket.Services.HitBtc
{
	public class StockSocketConnector : IStockSocketConnector
	{
		private ApiConnection _connection;

		public StockSocketConnector(ConfigurationService configurationService)
		{
			_connection = new ApiConnection(configurationService.GetExchangeConnectionSettings());
		}

		public void Connect()
		{
			var connectTask = ConnectAsync();
			connectTask.RunSynchronously();
		}

		public async Task ConnectAsync()
		{
			if (_connection == null)
				return;
			await _connection.Connect();
		}

		public async Task Disconnect()
		{
			if (_connection == null)
				return;
			await _connection.Disconnect();
			_connection = null;
		}

		public async Task<IList<Infrastructure.Common.Models.Market.CurrencyPair>> GetCurrencyPairs()
		{
			var request = new SingleSocketRequest<EmptyRequestParameters>
			{
				RequestMethodName = "getSymbols"
			};
			var currencyPairsInner = await _connection.DoRequest<CurrencyPair[]>(request);
			var currencyPairs = currencyPairsInner
				.Select(entity => entity.ToOuterModel())
				.ToList();

			return currencyPairs;
		}

		public async Task<Infrastructure.Common.Models.Market.CurrencyPair> GetCurrencyPair(string id)
		{
			var request = new SingleSocketRequest<CurrencyRequestParameters>
			{
				RequestMethodName = "getSymbol",
				RequestParameters = new CurrencyRequestParameters
				{
					CurrencyId = id
				}
			};
			var currencyPairInner = await _connection.DoRequest<CurrencyPair>(request);
			var currencyPair = currencyPairInner.ToOuterModel();

			return currencyPair;
		}

		public async Task SubscribeOnCandles(string currencyPairId, CandlePeriod period, Action<IList<Infrastructure.Common.Models.Market.Candle>> callback, int limit = 30)
		{
			var request = new SocketSubscriptionRequest<CandleRequestParameters>
			{
				RequestMethodName = "subscribeCandles",
				SnapshotMethodName = "snapshotCandles",
				NotificationMethodName = "updateCandles",
				UnsubscribeMethodName = "unsubscribeCandles",
				RequestParameters = new CandleRequestParameters
				{
					CurrencyPairId = currencyPairId,
					Period = period.ToInnerFormat(),
					Limit = limit
				}
			};

			await _connection.Subscribe<CandleNotificationParameters>(request, notificationParameters =>
			{
				var notificationCurrencyPairId = notificationParameters.CurrencyPairId;
				var notificationPeriod = CandlePeriodMap.ToOuterFormat(notificationParameters.Period);

				if (!(currencyPairId == notificationCurrencyPairId && notificationPeriod == period))
					return;

				callback(notificationParameters.Candles
					.Select(CandleMap.ToOuterModel)
					.ToList());
			});
		}

		public async Task SubscribeOnOrderBook(string currencyPairId, Action<IList<Infrastructure.Common.Models.Market.OrderBookItem>> callback)
		{
			var request = new SocketSubscriptionRequest<OrderBookRequestParameters>
			{
				RequestMethodName = "subscribeOrderbook",
				SnapshotMethodName = "snapshotOrderbook",
				NotificationMethodName = "updateOrderbook",
				UnsubscribeMethodName = "unsubscribeOrderbook",
				RequestParameters = new OrderBookRequestParameters
				{
					CurrencyPairId = currencyPairId,
				}
			};

			await _connection.Subscribe<OrderBookNotificationParameters>(request, notificationParameters =>
			{
				var notificationCurrencyPairId = notificationParameters.CurrencyPairId;

				if (currencyPairId != notificationCurrencyPairId)
					return;

				var mergedItems = new List<Infrastructure.Common.Models.Market.OrderBookItem>();

				mergedItems.AddRange(notificationParameters.AskItems.Select(item => item.ToOuterModel(OrderBookItemType.Ask)));
				mergedItems.AddRange(notificationParameters.BidItems.Select(item => item.ToOuterModel(OrderBookItemType.Bid)));

				callback(mergedItems);
			});
		}

		public async Task SubscribeOrders(Infrastructure.Common.Models.Market.CurrencyPair targetCurrencyPair, Action<Infrastructure.Common.Models.Trading.Order> callback)
		{
			var request = new SocketSubscriptionRequest<EmptyRequestParameters>
			{
				RequestMethodName = "subscribeReports",
				SnapshotMethodName = null,
				NotificationMethodName = "activeOrders",
				UnsubscribeMethodName = null,
				RequestParameters = new EmptyRequestParameters()
			};

			await _connection.Subscribe<Order[]>(request, orders =>
			{
				foreach (var order in orders)
				{
					if (order.CurrencyPairId != targetCurrencyPair.Id)
						continue;

					var result = order.ToOuterModel(targetCurrencyPair);

					callback(result);
				}
			});

			request = new SocketSubscriptionRequest<EmptyRequestParameters>
			{
				RequestMethodName = "subscribeReports",
				SnapshotMethodName = null,
				NotificationMethodName = "report",
				UnsubscribeMethodName = null,
				RequestParameters = new EmptyRequestParameters()
			};

			await _connection.Subscribe<Order>(request, order =>
			{
				if (order.CurrencyPairId != targetCurrencyPair.Id)
					return;

				var result = order.ToOuterModel(targetCurrencyPair);

				callback(result);
			});
		}

		public async Task RequestCancelOrder(Infrastructure.Common.Models.Trading.Order order)
		{
			var request = new SingleSocketRequest<ChangeOrderStateRequestParameters>
			{
				RequestMethodName = "cancelOrder",
				NeedResponse = false,
				RequestParameters = new ChangeOrderStateRequestParameters()
				{
					ExistingClientId = order.ClientId.ToString("N"),
				}
			};
			await _connection.DoRequest<Order>(request);
		}

		public async Task RequestReplaceOrder(Infrastructure.Common.Models.Trading.Order changedOrder, Guid newClientId, Action replacementErrorCallback)
		{
			var request = new SingleSocketRequest<ChangeOrderStateRequestParameters>
			{
				RequestMethodName = "cancelReplaceOrder",
				NeedResponse = false,
				RequestParameters = new ChangeOrderStateRequestParameters()
				{
					ExistingClientId = changedOrder.ClientId.ToString("N"),
					NewClientId = newClientId.ToString("N"),
					Price = changedOrder.Price,
					StopPrice = changedOrder.StopPrice ?? 0,
					Quantity = changedOrder.Quantity
				}
			};
			await _connection.DoRequest<Order>(request, replacementErrorCallback);
		}

		public void SubscribeErrors(Action<Exception> callback)
		{
			_connection.Error += (o, e) => callback((Exception)e.ExceptionObject);
		}
	}
}
