using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Enums;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Socket.Connection;
using StockManager.Infrastructure.Connectors.Socket.Models.NotificationParameters;
using StockManager.Infrastructure.Connectors.Socket.Models.RequestParameters;
using StockManager.Infrastructure.Connectors.Socket.Models.Market;
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

		public async Task SubscribeOrders(IList<Infrastructure.Common.Models.Market.CurrencyPair> targetCurrencyPairs, Action<Order> callback)
		{
			var request = new SocketSubscriptionRequest<EmptyRequestParameters>
			{
				RequestMethodName = "subscribeReports",
				SnapshotMethodName = null,
				NotificationMethodName = "report",
				UnsubscribeMethodName = null,
				RequestParameters = new EmptyRequestParameters()
			};

			await _connection.Subscribe<Models.Trading.Order>(request, order =>
			{
				var currencyPair = targetCurrencyPairs.FirstOrDefault(item => String.Equals(item.Id, order.CurrencyPairId, StringComparison.OrdinalIgnoreCase));

				var result = Models.Trading.OrderMap.ToOuterModel(order, currencyPair);

				callback(result);
			});
		}

		public void SubscribeErrors(Action<Exception> callback)
		{
			_connection.Error += (o, e) => callback((Exception)e.ExceptionObject);
		}
	}
}
