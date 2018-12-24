using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public interface ITradingDataConnector
	{
		Task<TradingBallance> GetTradingBallnce(string currencyId);
		Task<IList<Order>> GetActiveOrders(CurrencyPair currencyPair);
		Task<Order> GetOrderFromHistory(Guid clientOrderId, CurrencyPair currencyPair);
		Task<Order> CreateOrder(Order initialOrder, bool usePostOnly);
		Task<Order> CancelOrder(Order initialOrder);
	}
}
