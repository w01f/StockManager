using System.Collections.Generic;
using System.Linq;
using StockManager.Infrastructure.Common.Enums;

namespace StockManager.Infrastructure.Connectors.Rest.Models.Market
{
	static class OrderBookMap
	{
		public static IList<Infrastructure.Common.Models.Market.OrderBookItem> ToOuterModel(this OrderBook source)
		{
			var target = new List<Infrastructure.Common.Models.Market.OrderBookItem>();

			target.AddRange(source.AskItems?.Select(item => item.ToOuterModel(OrderBookItemType.Ask)) ??
				new Infrastructure.Common.Models.Market.OrderBookItem[] { });

			target.AddRange(source.BidItems?.Select(item => item.ToOuterModel(OrderBookItemType.Bid)) ??
							new Infrastructure.Common.Models.Market.OrderBookItem[] { });

			return target;
		}

		public static Infrastructure.Common.Models.Market.OrderBookItem ToOuterModel(this OrderBookItem source, OrderBookItemType itemType)
		{
			var target = new Infrastructure.Common.Models.Market.OrderBookItem()
			{
				Type = itemType,
				Price = source.Price,
				Size = source.Size,
			};
			return target;
		}
	}
}
