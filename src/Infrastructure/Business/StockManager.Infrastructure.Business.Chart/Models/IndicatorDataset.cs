using System.Collections.Generic;
using StockManager.Infrastructure.Analysis.Common.Models;

namespace StockManager.Infrastructure.Business.Chart.Models
{
	public class IndicatorDataset
	{
		public IndicatorSettings Settings { get; set; }
		public IList<BaseIndicatorValue> Values { get; set; }
	}
}
