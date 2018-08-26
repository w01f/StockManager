using System;
using StockManager.Domain.Core.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Trading.Settings
{
	public class TradingSettings
	{
		public DateTime Moment { get; set; }
		public string CurrencyPairId { get; set; }
		public CandlePeriod Period { get; set; }
		public int CandleRangeSize { get; set; }
		public OrderSide BaseOrderSide { get; set; }
		public decimal MaxOrderUsingBallncePart { get; set; } = 1;

		public OrderSide OppositeOrderSide => BaseOrderSide == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
	}
}
