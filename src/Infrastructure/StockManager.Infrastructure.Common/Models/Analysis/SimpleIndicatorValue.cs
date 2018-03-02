using System;

namespace StockManager.Infrastructure.Common.Models.Analysis
{
	public class SimpleIndicatorValue : BaseIndicatorValue
	{
		public decimal? Value { get; set; }

		public SimpleIndicatorValue(DateTime moment) : base(moment) { }
	}
}
