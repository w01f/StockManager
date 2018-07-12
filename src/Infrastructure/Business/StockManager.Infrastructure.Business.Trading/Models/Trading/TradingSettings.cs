using System;
using System.Collections.Generic;
using StockManager.Domain.Core.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Trading
{
	public class TradingSettings
	{
		public DateTime CurrentMoment { get; set; }
		public string CurrencyPairId { get; set; }
		public CandlePeriod Period { get; set; }
		public int CandleRangeSize { get; set; }

		public List<IndicatorSettings> IndicatorSettings { get; set; }

		public TradingSettings()
		{
			IndicatorSettings = new List<IndicatorSettings>();
		}
	}
}
