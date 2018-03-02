using System;

namespace StockManager.Infrastructure.Common.Models.Analysis
{
	public abstract class BaseIndicatorValue
	{
		public DateTime Moment { get; }

		protected BaseIndicatorValue(DateTime moment)
		{
			Moment = moment;
		}
	}
}
