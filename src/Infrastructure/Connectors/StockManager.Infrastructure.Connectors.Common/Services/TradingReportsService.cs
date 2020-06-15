using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Common;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public class TradingReportsService
	{
		private static readonly object OrdersAccessLocker = new object();

		private readonly IStockSocketConnector _stockSocketConnector;
		private readonly List<CurrencyPair> _existingCurrencyPairsInSubscription = new List<CurrencyPair>();

		public event EventHandler<TradingReportEventArgs> OrdersUpdated;

		public TradingReportsService(IStockSocketConnector tradingDataConnector)
		{
			_stockSocketConnector = tradingDataConnector ?? throw new ArgumentNullException(nameof(tradingDataConnector));
		}

		public async Task InitSubscription(IList<CurrencyPair> targetCurrencyPairs)
		{
			var newCurrencyPairs = targetCurrencyPairs.Where(targetPair => _existingCurrencyPairsInSubscription.All(existingPair => existingPair.Id != targetPair.Id)).ToList();

			await _stockSocketConnector.SubscribeOrders(newCurrencyPairs, order =>
			{
				lock (OrdersAccessLocker)
				{
					OnOrdersUpdated(order);
				}
			});
			_existingCurrencyPairsInSubscription.AddRange(newCurrencyPairs);
		}

		public async Task InitSubscription(CurrencyPair targetCurrencyPair)
		{
			await InitSubscription(new[] { targetCurrencyPair });
		}

		private void OnOrdersUpdated(Order order)
		{
			OrdersUpdated?.Invoke(this, new TradingReportEventArgs(order));
		}
	}
}
