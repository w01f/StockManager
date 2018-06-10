using System;

namespace StockManager.Infrastructure.Analysis.Common.Models
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
