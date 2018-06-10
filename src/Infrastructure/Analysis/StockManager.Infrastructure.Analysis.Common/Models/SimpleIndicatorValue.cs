using System;

namespace StockManager.Infrastructure.Analysis.Common.Models
{
	public class SimpleIndicatorValue : BaseIndicatorValue
	{
		public decimal? Value { get; set; }

		public SimpleIndicatorValue(DateTime moment) : base(moment) { }
	}
}
