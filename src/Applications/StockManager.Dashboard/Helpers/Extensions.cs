using System;
using System.Collections.Generic;
using StockManager.Infrastructure.Business.Common.Models.Chart;

namespace StockManager.Dashboard.Helpers
{
	static class Extensions
	{
		public static IEnumerable<string> GetIndicatorTitles(this BaseIndicatorSettings target)
		{
			switch (target.Type)
			{
				case IndicatorType.EMA:
					return new[] { String.Format("{0}{1}", target.Type.ToString(), target.Period) };
				case IndicatorType.Stochastic:
					return new[]
					{
						String.Format("{0}{1}K", target.Type.ToString(), target.Period),
						String.Format("{0}{1}D", target.Type.ToString(), target.Period),
					};
				default:
					throw new ArgumentOutOfRangeException("Undefined indicator type");
			}
		}
	}
}
