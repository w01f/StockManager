using System;
using StockManager.Infrastructure.Business.Common.Models.Chart;

namespace StockManager.Dashboard.Helpers
{
	static class Extensions
	{
		public static string GetIndicatorTitle(this IndicatorSettings target)
		{
			return String.Format("{0}{1}", target.Type.ToString(), target.Period);
		}
	}
}
