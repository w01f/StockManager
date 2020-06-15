using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public interface ITradingDataSocketConnector
	{
		Task SubscribeOrders(IList<CurrencyPair> targetCurrencyPairs, Action<Order> callback);
	}
}
