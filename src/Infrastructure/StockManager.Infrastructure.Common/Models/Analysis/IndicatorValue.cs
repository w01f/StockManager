using System;

namespace StockManager.Infrastructure.Common.Models.Analysis
{
	public class IndicatorValue
	{
		public DateTime Moment { get; }
		public decimal? Value { get; set; }

		public IndicatorValue(DateTime moment)
		{
			Moment = moment;
		}
	}
}
