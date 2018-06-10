using System;

namespace StockManager.Infrastructure.Analysis.Common.Models
{
	public class StochasticValue : BaseIndicatorValue
	{
		public decimal? K { get; set; }
		public decimal? D { get; set; }
		public decimal? J { get; set; }

		public StochasticValue(DateTime moment) : base(moment) { }
	}
}
