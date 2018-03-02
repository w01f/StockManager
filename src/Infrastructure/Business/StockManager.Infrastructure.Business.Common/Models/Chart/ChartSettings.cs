using System.Collections.Generic;
using StockManager.Domain.Core.Common.Enums;

namespace StockManager.Infrastructure.Business.Common.Models.Chart
{
	public class ChartSettings
	{
		public string CurrencyPairId { get; set; }
		public CandlePeriod Period { get; set; }
		public int CandleLimit { get; set; }
		public List<BaseIndicatorSettings> Indicators { get; set; }

		public ChartSettings()
		{
			CandleLimit = 100;
			Indicators = new List<BaseIndicatorSettings>();
		}
	}
}
