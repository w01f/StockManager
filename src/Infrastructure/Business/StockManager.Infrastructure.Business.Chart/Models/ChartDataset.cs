using System.Collections.Generic;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Business.Chart.Models
{
	public class ChartDataset
	{
		public List<Candle> Candles { get; set; }
		public List<IndicatorDataset> IndicatorData { get; }
		public List<TradingData> TradingData { get; set; }

		public ChartDataset()
		{
			IndicatorData = new List<IndicatorDataset>();
			TradingData = new List<TradingData>();
		}
	}
}
