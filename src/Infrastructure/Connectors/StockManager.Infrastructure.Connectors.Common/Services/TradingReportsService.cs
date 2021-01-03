using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Common;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public class TradingReportsService
	{
		private readonly IStockRestConnector _stockRestConnector;
		private readonly IStockSocketConnector _stockSocketConnector;
		private readonly ConcurrentDictionary<string, CurrencyPair> _existingCurrencyPairsInSubscription = new ConcurrentDictionary<string, CurrencyPair>();

		public event EventHandler<TradingReportEventArgs> OrdersUpdated;

		public TradingReportsService(IStockRestConnector stockRestConnector,
			IStockSocketConnector tradingDataConnector)
		{
			_stockRestConnector = stockRestConnector ?? throw new ArgumentNullException(nameof(stockRestConnector));
			_stockSocketConnector = tradingDataConnector ?? throw new ArgumentNullException(nameof(tradingDataConnector));
		}

		public async Task InitSubscription(string currencyPairId)
		{
			if (_existingCurrencyPairsInSubscription.ContainsKey(currencyPairId))
				return;

			var allCurrencyPairs = await _stockRestConnector.GetCurrencyPairs();
			var currencyPair = allCurrencyPairs.FirstOrDefault(item => item.Id == currencyPairId);
			if (currencyPair == null)
				throw new BusinessException($"Currency pair {currencyPairId} not found");

			await _stockSocketConnector.SubscribeOrders(currencyPair, OnOrdersUpdated);
			_existingCurrencyPairsInSubscription.TryAdd(currencyPairId, currencyPair);
		}

		private void OnOrdersUpdated(Order order)
		{
			OrdersUpdated?.Invoke(this, new TradingReportEventArgs(order));
		}
	}
}
