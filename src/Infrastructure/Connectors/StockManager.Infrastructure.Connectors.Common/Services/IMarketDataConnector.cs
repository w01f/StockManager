using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public interface IMarketDataConnector
	{
		Task<IList<CurrencyPair>> GetCurrensyPairs();
		Task<CurrencyPair> GetCurrensyPair(string id);
		Task<IList<Candle>> GetCandles(string currencyPairId, CandlePeriod period, int limit);
		Task<IList<OrderBookItem>> GetOrderBook(string currencyPairId, int limit);
		Task<IList<Ticker>> GetTickers();
		Task<Ticker> GetTicker(string currencyPairId);
	}
}
