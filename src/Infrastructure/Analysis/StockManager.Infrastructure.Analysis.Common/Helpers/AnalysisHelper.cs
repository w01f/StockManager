using System.Collections.Generic;
using System.Linq;

namespace StockManager.Infrastructure.Analysis.Common.Helpers
{
	public static class AnalysisHelper
	{
		public static IList<decimal> GetMinimumValues(this IList<decimal> range)
		{
			var values = new List<decimal>();
			for (int i = 0; i < range.Count; i++)
			{
				if (i == range.Count - 1) break;
				if (i == 0) continue;

				var value = range[i];
				if (value < range[i - 1] && value < range[i + 1])
					values.Add(value);
			}

			return values;
		}

		public static decimal GetAverageMinimum(this IList<decimal> range)
		{
			return range.GetMinimumValues().Average();
		}

		public static IList<decimal> GetMaximumValues(this IList<decimal> range)
		{
			var values = new List<decimal>();
			for (int i = 0; i < range.Count; i++)
			{
				if (i == range.Count - 1) break;
				if (i == 0) continue;

				var value = range[i];
				if (value > range[i - 1] && value > range[i + 1])
					values.Add(value);
			}

			return values;
		}

		public static decimal GetAverageMaximum(this IList<decimal> range)
		{
			return range.GetMinimumValues().Average();
		}
	}
}
