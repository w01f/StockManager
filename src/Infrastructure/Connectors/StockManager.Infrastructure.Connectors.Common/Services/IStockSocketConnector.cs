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
		void Connect();
		Task ConnectAsync();
		Task Disconnect();

		Task<IList<CurrencyPair>> GetCurrencyPairs();
		Task<CurrencyPair> GetCurrencyPair(string id);
		
		Task SubscribeOnCandles(string currencyPairId, CandlePeriod period, Action<IList<Candle>> callback, int limit = 30);
		Task SubscribeOnOrderBook(string currencyPairId, Action<IList<OrderBookItem>> callback);
		Task SubscribeOrders(CurrencyPair targetCurrencyPair, Action<Order> callback);

		Task RequestCancelOrder(Order order);
		Task RequestReplaceOrder(Order changedOrder, Guid newClientId, Action replacementErrorCallback);
		
		void SubscribeErrors(Action<Exception> callback);
	}
}
