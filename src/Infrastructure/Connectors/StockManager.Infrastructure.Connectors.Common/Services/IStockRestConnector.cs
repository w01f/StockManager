using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public interface IStockRestConnector
	{
		Task<IList<CurrencyPair>> GetCurrencyPairs();
		Task<CurrencyPair> GetCurrencyPair(string id);
		Task<IList<Candle>> GetCandles(string currencyPairId, CandlePeriod period, int limit);
		Task<IList<OrderBookItem>> GetOrderBook(string currencyPairId, OrderBookItemType itemType, int limit);
		Task<IList<Ticker>> GetTickers();
		Task<Ticker> GetTicker(string currencyPairId);
		Task<TradingBallance> GetTradingBalance(string currencyId);
		Task<IList<Order>> GetActiveOrders(CurrencyPair currencyPair);
		Task<Order> GetOrderFromHistory(Guid clientOrderId, CurrencyPair currencyPair);
		Task<Order> CreateOrder(Order initialOrder, bool usePostOnly);
		Task<Order> CancelOrder(Order initialOrder);
	}
}
