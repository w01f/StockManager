using System;
using System.Collections.Generic;
using StockManager.Domain.Core.Common.Enums;

namespace StockManager.Infrastructure.Business.Chart.Models
{
	public class ChartSettings
	{
		public string CurrencyPairId { get; set; }
		public CandlePeriod Period { get; set; }
		public int CandleLimit { get; set; }
		public DateTime CurrentMoment { get; set; }
		public List<IndicatorSettings> Indicators { get; set; }

		public ChartSettings()
		{
			CandleLimit = 100;
			Indicators = new List<IndicatorSettings>();
		}
	}
}
