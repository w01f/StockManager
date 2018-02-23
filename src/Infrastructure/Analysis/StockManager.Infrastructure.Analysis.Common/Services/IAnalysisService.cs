using System.Collections.Generic;
using StockManager.Infrastructure.Common.Models.Analysis;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Analysis.Common.Services
{
	public interface IAnalysisService
	{
		IList<IndicatorValue> ComputeEMA(IList<Candle> candles, int periodCount);
	}
}
