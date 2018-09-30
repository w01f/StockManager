using System;
using System.Collections.Generic;
using StockManager.Domain.Core.Enums;

namespace StockManager.Infrastructure.Utilities.Configuration.Models
{
	public class TradingSettings
	{
		public List<string> QuoteCurrencies { get; set; }

		public DateTime Moment { get; set; }
		public CandlePeriod Period { get; set; }
		public OrderSide BaseOrderSide { get; set; }

		public decimal MinCurrencyPairTradingVolumeInBTC { get; set; }
		public decimal MaxOrderUsingBallancePart { get; set; }
		public decimal StopLimitPriceDifferneceFactor { get; set; }

		public OrderSide OppositeOrderSide => BaseOrderSide == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
	}
}
