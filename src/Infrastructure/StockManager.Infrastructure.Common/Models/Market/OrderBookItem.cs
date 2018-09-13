using StockManager.Infrastructure.Common.Enums;

namespace StockManager.Infrastructure.Common.Models.Market
{
	public class OrderBookItem
	{
		public OrderBookItemType Type { get; set; }
		public decimal Price { get; set; }
		public decimal Size { get; set; }
	}
}
