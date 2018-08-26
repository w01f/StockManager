using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public interface ITradingDataConnector
	{
		Task<IList<TradingBallance>> GetTradingBallnce();
		Task<IList<Order>> GetActiveOrders(CurrencyPair currencyPair);
		Task<Order> GetOrder(Guid clinetOrderId);
		Task<Order> CreateOrder(Order order);
		Task<Order> CancelOrder(Order order);
	}
}
