using System.Data;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Infrastructure.Common.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Extensions.Connectors
{
	static class MarketDataConnectorExtensions
	{
		public static async Task<decimal> GetNearestBidSupportPrice(this OrderBookLoadingService orderBookLoadingService, CurrencyPair currencyPair)
		{
			var orderBookBidItems = (await orderBookLoadingService.GetOrderBook(currencyPair.Id, 20))
				.Where(item => item.Type == OrderBookItemType.Bid)
				.ToList();

			if (!orderBookBidItems.Any())
				throw new NoNullAllowedException("Couldn't load order book");

			var avgBidSize = orderBookBidItems
				.Average(item => item.Size);

			var topBidPrice = orderBookBidItems
				.OrderByDescending(item => item.Price)
				.Select(item => item.Price)
				.First();

			return orderBookBidItems
				.Where(item => item.Size > avgBidSize && item.Price < topBidPrice)
				.OrderByDescending(item => item.Price)
				.Select(item => item.Price)
				.First();
		}

		public static async Task<decimal> GetNearestAskSupportPrice(this OrderBookLoadingService orderBookLoadingService, CurrencyPair currencyPair)
		{
			var orderBookAskItems = (await orderBookLoadingService.GetOrderBook(currencyPair.Id, 20))
				.Where(item => item.Type == OrderBookItemType.Ask)
				.ToList();

			if (!orderBookAskItems.Any())
				throw new NoNullAllowedException("Couldn't load order book");

			var avgAskSize = orderBookAskItems
				.Average(item => item.Size);

			var bottomAskPrice = orderBookAskItems
				.OrderBy(item => item.Price)
				.Select(item => item.Price)
				.First();

			return orderBookAskItems
				.Where(item => item.Size > avgAskSize && item.Price > bottomAskPrice)
				.OrderBy(item => item.Price)
				.Select(item => item.Price)
				.First();
		}

		public static async Task<decimal> GetTopMeaningfulBidPrice(this OrderBookLoadingService orderBookLoadingService, CurrencyPair currencyPair)
		{
			var orderBookBidItems = (await orderBookLoadingService.GetOrderBook(currencyPair.Id, 5))
				.Where(item => item.Type == OrderBookItemType.Bid)
				.ToList();

			if (!orderBookBidItems.Any())
				throw new NoNullAllowedException("Couldn't load order book");

			var maxBidSize = orderBookBidItems
				.Max(item => item.Size);

			var topMeaningfulBidPrice = orderBookBidItems
				.Where(item => item.Size == maxBidSize)
				.OrderByDescending(item => item.Price)
				.Select(item => item.Price)
				.First();

			return topMeaningfulBidPrice;
		}

		public static async Task<decimal> GetTopBidPrice(this OrderBookLoadingService orderBookLoadingService, CurrencyPair currencyPair, int skip = 0)
		{
			var orderBookBidItems = (await orderBookLoadingService.GetOrderBook(currencyPair.Id, 5))
				.Where(item => item.Type == OrderBookItemType.Bid)
				.ToList();

			if (!orderBookBidItems.Any())
				throw new NoNullAllowedException("Couldn't load order book");

			return orderBookBidItems
				.OrderByDescending(item => item.Price)
				.Skip(skip)
				.Select(item => item.Price)
				.First();
		}

		public static async Task<decimal> GetBottomMeaningfulAskPrice(this OrderBookLoadingService orderBookLoadingService, CurrencyPair currencyPair)
		{
			var orderBookAskItems = (await orderBookLoadingService.GetOrderBook(currencyPair.Id, 5))
				.Where(item => item.Type == OrderBookItemType.Ask)
				.ToList();

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

		public static async Task<decimal> GetBottomAskPrice(this OrderBookLoadingService orderBookLoadingService, CurrencyPair currencyPair, int skip = 0)
		{
			var orderBookAskItems = (await orderBookLoadingService.GetOrderBook(currencyPair.Id, 5))
				.Where(item => item.Type == OrderBookItemType.Ask)
				.ToList();

			if (!orderBookAskItems.Any())
				throw new NoNullAllowedException("Couldn't load order book");

			var bottomMeaningfulAskPrice = orderBookAskItems
				.OrderBy(item => item.Price)
				.Skip(skip)
				.Select(item => item.Price)
				.First();

			return bottomMeaningfulAskPrice;
		}
	}
}
