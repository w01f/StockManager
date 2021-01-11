using System.Collections.Generic;
using System.Linq;
using StockManager.Infrastructure.Common.Enums;
using Tinkoff.Trading.OpenApi.Models;

namespace StockManager.Infrastructure.Connectors.Socket.Models.Tinkoff.Market
{
	static class OrderBookMap
	{
		public static IList<Infrastructure.Common.Models.Market.OrderBookItem> ToOuterModel(this Orderbook source)
		{
			var target = new List<Infrastructure.Common.Models.Market.OrderBookItem>();

			target.AddRange(source.Asks.Select(item => new Infrastructure.Common.Models.Market.OrderBookItem
			{
				Type = OrderBookItemType.Ask,
				Price = item.Price,
				Size = item.Quantity,
			}));

			target.AddRange(source.Bids.Select(item => new Infrastructure.Common.Models.Market.OrderBookItem
			{
				Type = OrderBookItemType.Bid,
				Price = item.Price,
				Size = item.Quantity,
			}));

			return target;
		}

		public static IList<Infrastructure.Common.Models.Market.OrderBookItem> ToOuterModel(this OrderbookPayload source)
		{
			var target = new List<Infrastructure.Common.Models.Market.OrderBookItem>();

			target.AddRange(source.Asks.Select(item => new Infrastructure.Common.Models.Market.OrderBookItem
			{
				Type = OrderBookItemType.Ask,
				Price = item.ElementAtOrDefault(0),
				Size = item.ElementAtOrDefault(1),
			}));

			target.AddRange(source.Bids.Select(item => new Infrastructure.Common.Models.Market.OrderBookItem
			{
				Type = OrderBookItemType.Bid,
				Price = item.ElementAtOrDefault(0),
				Size = item.ElementAtOrDefault(1),
			}));

			return target;
		}
	}
}
