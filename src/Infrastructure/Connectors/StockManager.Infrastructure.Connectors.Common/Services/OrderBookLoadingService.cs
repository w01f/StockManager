using System;
using System.Collections.Concurrent;
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
		private readonly IStockSocketConnector _stockSocketConnector;

		private readonly ConcurrentDictionary<string, List<OrderBookItem>> _orderBooks = new ConcurrentDictionary<string, List<OrderBookItem>>();

		public event EventHandler<OrderBookUpdatedEventArgs> OrderBookUpdated;

		public OrderBookLoadingService(IStockSocketConnector stockSocketConnector)
		{
			_stockSocketConnector = stockSocketConnector ?? throw new ArgumentNullException(nameof(stockSocketConnector));
		}

		public async Task InitSubscription(string currencyPairId)
		{
			if (_orderBooks.ContainsKey(currencyPairId))
				return;

			if (!_orderBooks.TryAdd(currencyPairId, new List<OrderBookItem>()))
				return;

			await _stockSocketConnector.SubscribeOnOrderBook(currencyPairId, newOrderBookItems =>
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
			});
		}

		public IList<OrderBookItem> GetOrderBook(string currencyPairId, OrderBookItemType itemType, int limit = 0)
		{
			if (_orderBooks.ContainsKey(currencyPairId) && _orderBooks[currencyPairId].Any())
				return _orderBooks[currencyPairId].Where(item => item.Type == itemType).ToList();
			return new List<OrderBookItem>();
		}

		private void OnOrderBookUpdated(string currencyPairId)
		{
			OrderBookUpdated?.Invoke(this, new OrderBookUpdatedEventArgs(currencyPairId));
		}
	}
}
