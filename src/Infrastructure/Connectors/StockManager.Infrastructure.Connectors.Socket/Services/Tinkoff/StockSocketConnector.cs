using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Socket.Models.Tinkoff.Market;
using StockManager.Infrastructure.Connectors.Socket.Models.Tinkoff.Subscriptions;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace StockManager.Infrastructure.Connectors.Socket.Services.Tinkoff
{
	public class StockSocketConnector : IStockSocketConnector
	{
		private readonly ConfigurationService _configurationService;

		private global::Tinkoff.Trading.OpenApi.Network.Connection _connection;

		private readonly ConcurrentBag<Subscription> _subscriptions = new ConcurrentBag<Subscription>();
		private bool _restoreConnection = true;

		public StockSocketConnector(ConfigurationService configurationService)
		{
			_configurationService = configurationService;
		}

		public void Connect()
		{
			if (_connection != null)
				return;

			var exchangeConnectionSettings = _configurationService.GetExchangeConnectionSettings();
			_connection = ConnectionFactory.GetConnection(exchangeConnectionSettings.ApiKey);
			_connection.StreamingClosed += async (o, e) => await Reconnect();
			_connection.WebSocketException += async (o, e) => await Reconnect();
		}

		public async Task ConnectAsync()
		{
			await Task.Run(() =>
			{
				Connect();
			});
		}

		public async Task Disconnect()
		{
			if (_connection == null)
				return;

			_restoreConnection = false;

			await CloseSubscriptions();

			_connection.Dispose();
			_connection = null;
		}

		public async Task<IList<CurrencyPair>> GetCurrencyPairs()
		{
			Connect();

			var marketInstrumentLists = await _connection.Context.MarketStocksAsync();
			var currencyPairs = marketInstrumentLists.Instruments
				.Select(entity => entity.ToOuterModel())
				.ToList();

			return currencyPairs;
		}

		public async Task<CurrencyPair> GetCurrencyPair(string id)
		{
			Connect();

			var marketInstrumentLists = await _connection.Context.MarketSearchByTickerAsync(id);
			var currencyPair = marketInstrumentLists.Instruments
				.Select(entity => entity.ToOuterModel())
				.FirstOrDefault();

			return currencyPair;
		}

		public async Task SubscribeOnCandles(string currencyPairId, CandlePeriod period, Action<IList<Candle>> callback, int limit = 30)
		{
			Connect();

			var innerPeriod = period.ToInnerFormat();

			var dateFrom = DateTime.UtcNow;
			switch (period)
			{
				case CandlePeriod.Minute1:
					dateFrom = dateFrom.AddMinutes(-(limit * 1));
					break;
				case CandlePeriod.Minute3:
					dateFrom = dateFrom.AddMinutes(-(limit * 3));
					break;
				case CandlePeriod.Minute5:
					dateFrom = dateFrom.AddMinutes(-(limit * 5));
					break;
				case CandlePeriod.Minute15:
					dateFrom = dateFrom.AddMinutes(-(limit * 15));
					break;
				case CandlePeriod.Minute30:
					dateFrom = dateFrom.AddMinutes(-(limit * 30));
					break;
				case CandlePeriod.Hour1:
					dateFrom = dateFrom.AddHours(-(limit * 1));
					break;
				case CandlePeriod.Hour4:
					dateFrom = dateFrom.AddHours(-(limit * 4));
					break;
				case CandlePeriod.Day1:
					dateFrom = dateFrom.AddDays(-(limit * 1));
					break;
				case CandlePeriod.Day7:
					dateFrom = dateFrom.AddDays(-(limit * 7));
					break;
				case CandlePeriod.Month1:
					dateFrom = dateFrom.AddMonths(-(limit * 1));
					break;
			}
			var dateTo = DateTime.UtcNow;

			var innerExistingCandles = await _connection.Context.MarketCandlesAsync(currencyPairId, dateFrom, dateTo, innerPeriod);
			var existingCandles = innerExistingCandles.Candles.Select(candle => candle.ToOuterModel()).ToList();
			callback(existingCandles);

			var subscription = new Subscription
			{
				SubscribeRequest = StreamingRequest.SubscribeCandle(currencyPairId, innerPeriod),
				UnsubscribeRequest = StreamingRequest.UnsubscribeCandle(currencyPairId, innerPeriod),
				EventHandler = (o, e) =>
				{
					if (!(e.Response is CandleResponse candleResponse) ||
						candleResponse.Payload.Figi != currencyPairId ||
						candleResponse.Payload.Interval != innerPeriod)
						return;

					var candle = candleResponse.Payload.ToOuterModel();
					callback(new List<Candle> { candle });
				}
			};

			_connection.StreamingEventReceived += (o, e) => subscription.EventHandler(o, e);
			await _connection.Context.SendStreamingRequestAsync(subscription.SubscribeRequest);
		}

		public async Task SubscribeOnOrderBook(string currencyPairId, Action<IList<OrderBookItem>> callback)
		{
			Connect();

			var innerExistingOrderbook = await _connection.Context.MarketOrderbookAsync(currencyPairId, 30);
			var existingOrderBook = innerExistingOrderbook.ToOuterModel();
			callback(existingOrderBook);

			var subscription = new Subscription
			{
				SubscribeRequest = StreamingRequest.SubscribeOrderbook(currencyPairId, 30),
				UnsubscribeRequest = StreamingRequest.UnsubscribeOrderbook(currencyPairId, 30),
				EventHandler = (o, e) =>
				{
					if (!(e.Response is OrderbookResponse orderbookResponse) ||
						orderbookResponse.Payload.Figi != currencyPairId)
						return;

					var orderBook = orderbookResponse.Payload.ToOuterModel();
					callback(orderBook);
				}
			};

			_connection.StreamingEventReceived += (o, e) => subscription.EventHandler(o, e);
			await _connection.Context.SendStreamingRequestAsync(subscription.SubscribeRequest);
		}

		public Task SubscribeOrders(CurrencyPair targetCurrencyPair, Action<Infrastructure.Common.Models.Trading.Order> callback)
		{
			throw new NotImplementedException();
		}

		public Task RequestCancelOrder(Infrastructure.Common.Models.Trading.Order order)
		{
			throw new NotImplementedException();
		}

		public Task RequestReplaceOrder(Infrastructure.Common.Models.Trading.Order changedOrder, Guid newClientId, Action replacementErrorCallback)
		{
			throw new NotImplementedException();
		}

		public void SubscribeErrors(Action<Exception> callback)
		{
			_connection.WebSocketException += (o, e) => callback(e.InnerException);
		}

		private async Task Reconnect()
		{
			if (!_restoreConnection)
				return;

			_connection = null;

			Connect();
			await RestoreSubscriptions();
		}

		private async Task RestoreSubscriptions()
		{
			foreach (var subscription in _subscriptions)
			{
				await _connection.SendStreamingRequestAsync(subscription.SubscribeRequest);
				_connection.StreamingEventReceived += (o, e) => subscription.EventHandler(o, e);
			}
		}

		private async Task CloseSubscriptions()
		{
			while (!_subscriptions.IsEmpty)
			{
				if (_subscriptions.TryTake(out var subscription))
					await _connection.SendStreamingRequestAsync(subscription.UnsubscribeRequest);
			}
		}
	}
}
