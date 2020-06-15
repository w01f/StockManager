using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Infrastructure.Common.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Common;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public class OrderBookLoadingService
	{
		private static readonly object OrderBookAccessLocker = new object();

		private readonly IStockRestConnector _stockRestConnector;
		private readonly IStockSocketConnector _stockSocketConnector;

		private readonly IDictionary<string, List<OrderBookItem>> _orderBooks = new Dictionary<string, List<OrderBookItem>>();

		public event EventHandler<OrderBookUpdatedEventArgs> OrderBookUpdated;

		public OrderBookLoadingService(IStockRestConnector stockRestConnector,
			IStockSocketConnector stockSocketConnector)
		{
			_stockRestConnector = stockRestConnector ?? throw new ArgumentNullException(nameof(stockRestConnector));
			_stockSocketConnector = stockSocketConnector ?? throw new ArgumentNullException(nameof(stockSocketConnector));
		}

		public async Task InitSubscription(string currencyPairId)
		{
			if (_orderBooks.ContainsKey(currencyPairId))
				return;

			_orderBooks.Add(currencyPairId, new List<OrderBookItem>());

			await _stockSocketConnector.SubscribeOnOrderBook(currencyPairId, newOrderBookItems =>
			{
				lock (OrderBookAccessLocker)
				{
					var orderBookItems = _orderBooks[currencyPairId].ToList();
					orderBookItems.AddRange(newOrderBookItems);

					_orderBooks[currencyPairId].Clear();
					_orderBooks[currencyPairId].AddRange(orderBookItems
						.Where(item => item.Type == OrderBookItemType.Ask && item.Size > 0)
						.OrderBy(item => item.Price)
						.Take(20));
					_orderBooks[currencyPairId].AddRange(orderBookItems
						.Where(item => item.Type == OrderBookItemType.Bid && item.Size > 0)
						.OrderByDescending(item => item.Price)
						.Take(20));

					OnOrderBookUpdated(currencyPairId);
				}
			});
		}

		public async Task<IList<OrderBookItem>> GetOrderBook(string currencyPairId, OrderBookItemType itemType, int limit = 0)
		{
			if (_orderBooks.ContainsKey(currencyPairId) && _orderBooks[currencyPairId].Any())
				return _orderBooks[currencyPairId].Where(item => item.Type == itemType).ToList();

			return await _stockRestConnector.GetOrderBook(currencyPairId, itemType, limit);
		}

		private void OnOrderBookUpdated(string currencyPairId)
		{
			OrderBookUpdated?.Invoke(this, new OrderBookUpdatedEventArgs(currencyPairId));
		}
	}
}
