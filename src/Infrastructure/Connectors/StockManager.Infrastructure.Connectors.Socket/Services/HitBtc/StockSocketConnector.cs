using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Enums;
using StockManager.Infrastructure.Connectors.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Socket.Connection;
using StockManager.Infrastructure.Connectors.Socket.Models.NotificationParameters;
using StockManager.Infrastructure.Connectors.Socket.Models.RequestParameters;
using StockManager.Infrastructure.Connectors.Socket.Models.Market;
using StockManager.Infrastructure.Connectors.Socket.Models.Trading;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Connectors.Socket.Services.HitBtc
{
	public class StockSocketConnector : IStockSocketConnector
	{
		private HitBtcConnection _connection;

		public StockSocketConnector(ConfigurationService configurationService)
		{
			_connection = new HitBtcConnection(configurationService.GetExchangeConnectionSettings());
		}

		public async Task Connect()
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

		public async Task SubscribeOnTickers(string currencyPairId, Action<Infrastructure.Common.Models.Market.Ticker> callback)
		{
			var request = new SocketSubscriptionRequest<TickerRequestParameters>
			{
				RequestMethodName = "subscribeTicker",
				SnapshotMethodName = null,
				NotificationMethodName = "ticker",
				UnsubscribeMethodName = "unsubscribeTicker",
				RequestParameters = new TickerRequestParameters
				{
					CurrencyPairId = currencyPairId,
				}
			};

			await _connection.Subscribe<Ticker>(request, ticker =>
			{
				callback(ticker.ToOuterModel());
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

		public async Task<Infrastructure.Common.Models.Trading.Order> CreateOrder(Infrastructure.Common.Models.Trading.Order order, bool usePostOnly)
		{
			var orderInner = order.ToInnerModel();
			orderInner.PostOnly = usePostOnly;

			var request = new SingleSocketRequest<CreateOrderRequestParameters>
			{
				RequestMethodName = "newOrder",
				NeedResponse = false,
				RequestParameters = new CreateOrderRequestParameters
				{
					ClientId = orderInner.ClientId,
					CurrencyPairId = orderInner.CurrencyPairId,
					Price = orderInner.Price,
					Quantity = orderInner.Quantity,
					OrderSide = orderInner.OrderSide,
					StopPrice = orderInner.StopPrice,
					OrderType = orderInner.OrderType,
					ExpireTime = orderInner.ExpireTime,
					TimeInForce = orderInner.TimeInForce,
					PostOnly = usePostOnly,
				}
			};
			await _connection.DoRequest<Order>(request);
			return order;
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
