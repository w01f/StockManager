using System;

namespace StockManager.Infrastructure.Common.Models.Market
{
	public class Ticker
	{
		public string CurrencyPairId { get; set; }
		public decimal BestAskPrice { get; set; }
		public decimal BestBidPrice { get; set; }
		public decimal LastPrice { get; set; }
		public decimal OpenPrice { get; set; }
		public decimal MinPrice { get; set; }
		public decimal MaxPrice { get; set; }
		public decimal VolumeInBaseCurrency { get; set; }
		public decimal VolumeInQuoteCurrency { get; set; }
		public DateTime Updated { get; set; }
	}
}
