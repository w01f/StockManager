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
		private readonly IMarketDataRestConnector _marketDataRestConnector;
		private readonly IMarketDataSocketConnector _marketDataSocketConnector;

		private readonly IDictionary<string, List<OrderBookItem>> _orderBooks = new Dictionary<string, List<OrderBookItem>>();

		public event EventHandler<OrderBookUpdatedEventArgs> OrderBookUpdated;

		public OrderBookLoadingService(IMarketDataRestConnector marketDataRestConnector,
			IMarketDataSocketConnector marketDataSocketConnector)
		{
			_marketDataRestConnector = marketDataRestConnector ?? throw new ArgumentNullException(nameof(marketDataRestConnector));
			_marketDataSocketConnector = marketDataSocketConnector ?? throw new ArgumentNullException(nameof(marketDataSocketConnector));
		}

		public void InitSubscription(string currencyPairId)
		{
			if (_orderBooks.ContainsKey(currencyPairId))
				return;

			_orderBooks.Add(currencyPairId, new List<OrderBookItem>());

			_marketDataSocketConnector.SubscribeOnOrderBook(currencyPairId, newOrderBookItems =>
			{
				var orderBookItems = _orderBooks[currencyPairId].ToList();
				orderBookItems.AddRange(newOrderBookItems);

				_orderBooks[currencyPairId].Clear();
				_orderBooks[currencyPairId].AddRange(orderBookItems
					.Where(item => item.Type == OrderBookItemType.Ask && item.Size > 0)
					.OrderByDescending(item => item.Price)
					.Take(20));
				_orderBooks[currencyPairId].AddRange(orderBookItems
					.Where(item => item.Type == OrderBookItemType.Bid && item.Size > 0)
					.OrderByDescending(item => item.Price)
					.Take(20));

				OnOrderBookUpdated(currencyPairId);
			});
		}

		public async Task<IList<OrderBookItem>> GetOrderBook(string currencyPairId, int limit = 0)
		{
			if (_orderBooks.ContainsKey(currencyPairId))
				return _orderBooks[currencyPairId];

			return await _marketDataRestConnector.GetOrderBook(currencyPairId, limit);
		}

		private void OnOrderBookUpdated(string currencyPairId)
		{
			OrderBookUpdated?.Invoke(this, new OrderBookUpdatedEventArgs(currencyPairId));
		}
	}
}
