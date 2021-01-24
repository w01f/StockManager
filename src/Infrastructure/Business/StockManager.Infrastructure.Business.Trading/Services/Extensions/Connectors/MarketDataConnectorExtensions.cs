using System.Data;
using System.Linq;
using StockManager.Infrastructure.Common.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Extensions.Connectors
{
	static class MarketDataConnectorExtensions
	{
		public static decimal GetNearestBidSupportPrice(this OrderBookLoadingService orderBookLoadingService, CurrencyPair currencyPair)
		{
			var orderBookBidItems = orderBookLoadingService.GetOrderBook(currencyPair.Id, OrderBookItemType.Bid, 20).ToList();

			if (!orderBookBidItems.Any())
				throw new NoNullAllowedException("Couldn't load order book");

			var avgBidSize = orderBookBidItems
				.Average(item => item.Size);

			var topBidPrice = orderBookBidItems
				.OrderByDescending(item => item.Price)
				.Select(item => item.Price)
				.First();

			return orderBookBidItems
				.Where(item => item.Size > avgBidSize && item.Price <= topBidPrice)
				.OrderByDescending(item => item.Price)
				.Select(item => item.Price)
				.First();
		}

		public static decimal GetNearestAskSupportPrice(this OrderBookLoadingService orderBookLoadingService, CurrencyPair currencyPair)
		{
			var orderBookAskItems = orderBookLoadingService.GetOrderBook(currencyPair.Id, OrderBookItemType.Ask, 20).ToList();

			if (!orderBookAskItems.Any())
				throw new NoNullAllowedException("Couldn't load order book");

			var avgAskSize = orderBookAskItems
				.Average(item => item.Size);

			var bottomAskPrice = orderBookAskItems
				.OrderBy(item => item.Price)
				.Select(item => item.Price)
				.First();

			return orderBookAskItems
				.Where(item => item.Size > avgAskSize && item.Price >= bottomAskPrice)
				.OrderBy(item => item.Price)
				.Select(item => item.Price)
				.First();
		}

		public static decimal GetTopMeaningfulBidPrice(this OrderBookLoadingService orderBookLoadingService, CurrencyPair currencyPair)
		{
			var orderBookBidItems = orderBookLoadingService.GetOrderBook(currencyPair.Id, OrderBookItemType.Bid, 5).ToList();

			if (!orderBookBidItems.Any())
				return 0;

			var maxBidSize = orderBookBidItems
				.Max(item => item.Size);

			var topMeaningfulBidPrice = orderBookBidItems
				.Where(item => item.Size == maxBidSize)
				.OrderByDescending(item => item.Price)
				.Select(item => item.Price)
				.First();

			return topMeaningfulBidPrice;
		}

		public static decimal GetTopBidPrice(this OrderBookLoadingService orderBookLoadingService, CurrencyPair currencyPair, int skip = 0)
		{
			var orderBookBidItems = orderBookLoadingService.GetOrderBook(currencyPair.Id, OrderBookItemType.Bid, 5).ToList();

			if (!orderBookBidItems.Any())
				throw new NoNullAllowedException("Couldn't load order book");

			return orderBookBidItems
				.OrderByDescending(item => item.Price)
				.Skip(skip)
				.Select(item => item.Price)
				.First();
		}

		public static decimal GetBottomMeaningfulAskPrice(this OrderBookLoadingService orderBookLoadingService, CurrencyPair currencyPair)
		{
			var orderBookAskItems = orderBookLoadingService.GetOrderBook(currencyPair.Id, OrderBookItemType.Ask, 5).ToList();

			if (!orderBookAskItems.Any())
				throw new NoNullAllowedException("Couldn't load order book");

			var maxAskSize = orderBookAskItems
				.Max(item => item.Size);

			var bottomMeaningfulAskPrice = orderBookAskItems
				.Where(item => item.Size == maxAskSize)
				.OrderBy(item => item.Price)
				.Select(item => item.Price)
				.First();

			return bottomMeaningfulAskPrice;
		}

		public static decimal GetBottomAskPrice(this OrderBookLoadingService orderBookLoadingService, CurrencyPair currencyPair, int skip = 0)
		{
			var orderBookAskItems = orderBookLoadingService.GetOrderBook(currencyPair.Id, OrderBookItemType.Ask, 5).ToList();

			if (!orderBookAskItems.Any())
				return 0;

			var bottomMeaningfulAskPrice = orderBookAskItems
				.OrderBy(item => item.Price)
				.Skip(skip)
				.Select(item => item.Price)
				.First();

			return bottomMeaningfulAskPrice;
		}
	}
}
