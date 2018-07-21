using System;
using StockManager.Domain.Core.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Trading
{
	public class TradingSettings
	{
		public DateTime CurrentMoment { get; set; }
		public string CurrencyPairId { get; set; }
		public CandlePeriod Period { get; set; }
		public int CandleRangeSize { get; set; }
	}
}
