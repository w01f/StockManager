using System.Collections.Generic;
using StockManager.Infrastructure.Common.Models.Analysis;

namespace StockManager.Infrastructure.Business.Common.Models.Chart
{
	public class IndicatorDataset
	{
		public IndicatorSettings Settings { get; set; }
		public IList<BaseIndicatorValue> Values { get; set; }
	}
}
