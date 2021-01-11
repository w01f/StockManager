using StockManager.Infrastructure.Common.Enums;

namespace StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.Market
{
	static class OrderBookMap
	{
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
