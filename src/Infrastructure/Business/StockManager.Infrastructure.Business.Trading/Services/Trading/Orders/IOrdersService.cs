using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Orders
{
	public interface IOrdersService
	{
		Task<IList<Order>> GetActiveOrders(Infrastructure.Common.Models.Market.CurrencyPair currencyPair);
		Task<Order> GetOrderFromHistory(Guid clientOrderId, Infrastructure.Common.Models.Market.CurrencyPair currencyPair);
		Task<Order> CreateBuyLimitOrder(Order order);
		Task<Order> CreateSellLimitOrder(Order order);
		Task<Order> CreateSellMarketOrder(Order order);
		Task RequestReplaceOrder(Order order, Guid newClientId, Action replacementErrorCallback);
		Task RequestCancelOrder(Order order);
		Task<Order> CancelOrder(Order order);
	}
}
