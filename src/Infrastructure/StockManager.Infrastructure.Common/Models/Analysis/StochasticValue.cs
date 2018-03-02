using System;

namespace StockManager.Infrastructure.Common.Models.Analysis
{
	public class StochasticValue : BaseIndicatorValue
	{
		public decimal? K { get; set; }
		public decimal? D { get; set; }
		public decimal? J { get; set; }

		public StochasticValue(DateTime moment) : base(moment) { }
	}
}
