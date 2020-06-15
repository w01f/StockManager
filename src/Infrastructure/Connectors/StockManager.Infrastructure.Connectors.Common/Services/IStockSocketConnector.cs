using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public interface IStockSocketConnector
	{
		Task Connect();
		Task Disconnect();
		
		Task SubscribeOnCandles(string currencyPairId, CandlePeriod period, Action<IList<Candle>> callback, int limit = 30);
		Task SubscribeOnTickers(string currencyPairId, Action<Ticker> callback);
		Task SubscribeOnOrderBook(string currencyPairId, Action<IList<OrderBookItem>> callback);
		Task SubscribeOrders(IList<CurrencyPair> targetCurrencyPairs, Action<Order> callback);
		
		void SubscribeErrors(Action<Exception> callback);
	}
}
