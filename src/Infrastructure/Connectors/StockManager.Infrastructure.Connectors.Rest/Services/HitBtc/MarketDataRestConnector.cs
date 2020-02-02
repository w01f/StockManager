using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Rest.Connection;
using StockManager.Infrastructure.Connectors.Rest.Models.Market;

namespace StockManager.Infrastructure.Connectors.Rest.Services.HitBtc
{
	public class MarketDataRestConnector : IMarketDataRestConnector
	{
		public async Task<IList<Infrastructure.Common.Models.Market.CurrencyPair>> GetCurrensyPairs()
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

		public async Task<Infrastructure.Common.Models.Market.CurrencyPair> GetCurrensyPair(string id)
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

			var response = await connection.DoRequest(request);
			var candles = response
				.ExtractResponseData<Candle[]>()
				.Select(entity => entity.ToOuterModel())
				.ToList();

			return candles;
		}

		public async Task<IList<Infrastructure.Common.Models.Market.OrderBookItem>> GetOrderBook(string currencyPairId, int limit)
		{
			var connection = new HitBtcConnection();
			var request = new RestRequest(String.Format("public/orderbook/{0}", currencyPairId), Method.GET);
			request.ConfigureRequest();

			request.AddParameter("limit", limit);

			var response = await connection.DoRequest(request);
			var orderBookItems = response
				.ExtractResponseData<OrderBook>()
				?.ToOuterModel();

			return orderBookItems ?? new Infrastructure.Common.Models.Market.OrderBookItem[] { };
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
	}
}
