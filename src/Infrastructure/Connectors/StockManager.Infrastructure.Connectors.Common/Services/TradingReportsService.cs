using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Common;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public class TradingReportsService
	{
		private readonly ITradingDataConnector _tradingDataConnector;
		
		public event EventHandler<TradingReportEventArgs> OrdersUpdated;

		public TradingReportsService(ITradingDataConnector tradingDataConnector)
		{
			_tradingDataConnector = tradingDataConnector ?? throw new ArgumentNullException(nameof(tradingDataConnector));
		}

		public async Task InitSubscription(IList<CurrencyPair> targetCurrencyPairs)
		{
			await _tradingDataConnector.SubscribeOrders(targetCurrencyPairs, OnOrdersUpdated);
		}

		private void OnOrdersUpdated(Order order)
		{
			OrdersUpdated?.Invoke(this, new TradingReportEventArgs(order));
		}
	}
}
