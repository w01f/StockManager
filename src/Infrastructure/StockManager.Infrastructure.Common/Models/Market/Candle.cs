using System;

namespace StockManager.Infrastructure.Common.Models.Market
{
	public class Candle
	{
		public DateTime Moment { get; set; }
		public decimal OpenPrice { get; set; }
		public decimal ClosePrice { get; set; }
		public decimal MaxPrice { get; set; }
		public decimal MinPrice { get; set; }
		public decimal VolumeInBaseCurrency { get; set; }
		public decimal VolumeInQuoteCurrency { get; set; }

		public bool IsRisingCandle => ClosePrice > OpenPrice;
		public bool IsFallingCandle => ClosePrice <= OpenPrice;
	}
}
