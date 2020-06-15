using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Enums;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Rest.Connection;
using StockManager.Infrastructure.Connectors.Rest.Models.Market;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Connectors.Rest.Models.Trading;

namespace StockManager.Infrastructure.Connectors.Rest.Services.HitBtc
{
	public class StockRestConnector : IStockRestConnector
	{
		private readonly ConfigurationService _configurationService;

		public StockRestConnector(ConfigurationService configurationService)
		{
			_configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
		}

		public async Task<IList<Infrastructure.Common.Models.Market.CurrencyPair>> GetCurrencyPairs()
		{
			var connection = new HitBtcConnection();
			var request = new RestRequest("public/symbol", Method.GET);
			request.ConfigureRequest();
			var response = await connection.DoRequest(request);
			var currencyPairs = response
				.ExtractResponseData<CurrencyPair[]>()
				.Select(entity => entity.ToOuterModel())
				.ToList();
			return currencyPairs;
		}

		public async Task<Infrastructure.Common.Models.Market.CurrencyPair> GetCurrencyPair(string id)
		{
			var connection = new HitBtcConnection();
			var request = new RestRequest(String.Format("public/symbol/{0}", id), Method.GET);
			request.ConfigureRequest();
			var response = await connection.DoRequest(request);
			var currencyPair = response
				.ExtractResponseData<CurrencyPair>()
				?.ToOuterModel();
			return currencyPair;
		}

		public async Task<IList<Infrastructure.Common.Models.Market.Candle>> GetCandles(String currencyPairId, CandlePeriod period, int limit)
		{
			var connection = new HitBtcConnection();
			var request = new RestRequest(String.Format("public/candles/{0}", currencyPairId), Method.GET);
			request.ConfigureRequest();

			var candlePeriod = period.ToInnerFormat();
			request.AddParameter("period", candlePeriod);

			request.AddParameter("limit", limit);
			request.AddParameter("sort", "DESC");

			var response = await connection.DoRequest(request);
			var candles = response
				.ExtractResponseData<Candle[]>()
				.Select(entity => entity.ToOuterModel())
				.ToList();

			return candles;
		}

		public async Task<IList<Infrastructure.Common.Models.Market.OrderBookItem>> GetOrderBook(string currencyPairId, OrderBookItemType itemType, int limit)
		{
			var connection = new HitBtcConnection();
			var request = new RestRequest(String.Format("public/orderbook/{0}", currencyPairId), Method.GET);
			request.ConfigureRequest();

			request.AddParameter("limit", limit);
			request.AddParameter("sort", "ASC");

			var response = await connection.DoRequest(request);
			var orderBookItems = response
				.ExtractResponseData<OrderBook>()
				?.ToOuterModel();

			return (orderBookItems ?? new Infrastructure.Common.Models.Market.OrderBookItem[] { }).Where(item => item.Type == itemType).ToList();
		}

		public async Task<IList<Infrastructure.Common.Models.Market.Ticker>> GetTickers()
		{
			var connection = new HitBtcConnection();
			var request = new RestRequest("public/ticker", Method.GET);
			request.ConfigureRequest();
			var response = await connection.DoRequest(request);
			var tickers = response
				.ExtractResponseData<Ticker[]>()
				.Select(entity => entity.ToOuterModel())
				.ToList();
			return tickers;
		}

		public async Task<Infrastructure.Common.Models.Market.Ticker> GetTicker(string currencyPairId)
		{
			var connection = new HitBtcConnection();
			var request = new RestRequest(String.Format("public/ticker/{0}", currencyPairId), Method.GET);
			request.ConfigureRequest();
			var response = await connection.DoRequest(request);
			var ticker = response
				.ExtractResponseData<Ticker>()
				?.ToOuterModel();
			return ticker;
		}

		public async Task<Infrastructure.Common.Models.Trading.TradingBallance> GetTradingBalance(string currencyId)
		{
			var connection = new HitBtcConnection();
			var request = new RestRequest("trading/balance", Method.GET);
			request.ConfigureRequest();

			var exchangeConnectionSettings = _configurationService.GetExchangeConnectionSettings();

			var response = await connection.DoRequest(
				request,
				exchangeConnectionSettings.ApiKey,
				exchangeConnectionSettings.SecretKey);

			var tradingBallance = response
				.ExtractResponseData<TradingBallance[]>()
				.Where(entity => String.Equals(entity.CurrencyId, currencyId, StringComparison.OrdinalIgnoreCase))
				.Select(entity => entity.ToOuterModel())
				.FirstOrDefault();
			return tradingBallance;
		}

		public async Task<IList<Infrastructure.Common.Models.Trading.Order>> GetActiveOrders(Infrastructure.Common.Models.Market.CurrencyPair currencyPair)
		{
			var connection = new HitBtcConnection();
			var request = new RestRequest("order", Method.GET);
			request.ConfigureRequest();

			var exchangeConnectionSettings = _configurationService.GetExchangeConnectionSettings();

			var response = await connection.DoRequest(
				request,
				exchangeConnectionSettings.ApiKey,
				exchangeConnectionSettings.SecretKey);

			var orders = response
				.ExtractResponseData<Order[]>()
				.Where(order => order.CurrencyPairId == currencyPair.Id)
				.Select(entity => entity.ToOuterModel(currencyPair))
				.ToList();

			return orders;
		}

		public async Task<Infrastructure.Common.Models.Trading.Order> GetOrderFromHistory(Guid clientOrderId, Infrastructure.Common.Models.Market.CurrencyPair currencyPair)
		{
			var connection = new HitBtcConnection();
			var request = new RestRequest("history/order", Method.GET);
			request.ConfigureRequest();

			request.AddParameter("clientOrderId", clientOrderId.ToString("N"));

			var exchangeConnectionSettings = _configurationService.GetExchangeConnectionSettings();

			var response = await connection.DoRequest(
				request,
				exchangeConnectionSettings.ApiKey,
				exchangeConnectionSettings.SecretKey);

			var order = response
				.ExtractResponseData<Order[]>()
				.Select(entity => entity.ToOuterModel(currencyPair))
				.FirstOrDefault();
			return order;
		}

		public async Task<Infrastructure.Common.Models.Trading.Order> CreateOrder(Infrastructure.Common.Models.Trading.Order initialOrder, bool usePostOnly = false)
		{
			var innerModel = initialOrder.ToInnerModel();

			var connection = new HitBtcConnection();
			var request = new RestRequest("order", Method.POST);
			request.ConfigureRequest();

			request.AddJsonBody(new
			{
				clientOrderId = innerModel.ClientId,
				symbol = innerModel.CurrencyPairId,
				side = innerModel.OrderSide,
				type = innerModel.OrderType,
				timeInForce = innerModel.TimeInForce,
				quantity = innerModel.Quantity.ToString("#0.########################"),
				price = innerModel.Price.ToString("#0.########################"),
				stopPrice = innerModel.StopPrice.ToString("#0.########################"),
				postOnly = usePostOnly
			});

			var exchangeConnectionSettings = _configurationService.GetExchangeConnectionSettings();

			var response = await connection.DoRequest(
				request,
				exchangeConnectionSettings.ApiKey,
				exchangeConnectionSettings.SecretKey);

			var responseOrder = response
				.ExtractResponseData<Order>()
				?.ToOuterModel(initialOrder.CurrencyPair);
			return responseOrder;
		}

		public async Task<Infrastructure.Common.Models.Trading.Order> CancelOrder(Infrastructure.Common.Models.Trading.Order initialOrder)
		{
			var innerModel = initialOrder.ToInnerModel();

			var connection = new HitBtcConnection();
			var request = new RestRequest(String.Format("order/{0}", innerModel.ClientId), Method.DELETE);
			request.ConfigureRequest();

			var exchangeConnectionSettings = _configurationService.GetExchangeConnectionSettings();

			var response = await connection.DoRequest(
				request,
				exchangeConnectionSettings.ApiKey,
				exchangeConnectionSettings.SecretKey);

			var responseOrder = response
				.ExtractResponseData<Order>()
				?.ToOuterModel(initialOrder.CurrencyPair);
			return responseOrder;
		}
	}
}
