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
		public static async Task<decimal> GetNearestBidSupportPrice(this IMarketDataRestConnector marketDataRestConnector, CurrencyPair currencyPair)
		{
			var orderBookBidItems = (await marketDataRestConnector.GetOrderBook(currencyPair.Id, 20))
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

		public static async Task<decimal> GetNearestAskSupportPrice(this IMarketDataRestConnector marketDataRestConnector, CurrencyPair currencyPair)
		{
			var orderBookAskItems = (await marketDataRestConnector.GetOrderBook(currencyPair.Id, 20))
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
	}
}
