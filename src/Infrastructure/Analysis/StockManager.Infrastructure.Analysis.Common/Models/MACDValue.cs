﻿using System;

namespace StockManager.Infrastructure.Analysis.Common.Models
{
	public class MACDValue : BaseIndicatorValue
	{
		public decimal? MACD { get; set; }
		public decimal? Signal { get; set; }
		public decimal? Histogram { get; set; }

		public MACDValue(DateTime moment) : base(moment) { }
	}
}
