using System;

namespace StockManager.Infrastructure.Business.Chart.Models
{
	public class TradingData
	{
		public DateTime Moment { get; set; }
		public decimal? BuyPrice { get; set; }
		public decimal? SellPrice { get; set; }
	}
}
