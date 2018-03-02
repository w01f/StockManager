using System.Collections.Generic;
using StockManager.Infrastructure.Common.Models.Analysis;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Analysis.Common.Services
{
	public interface IIndicatorComputingService
	{
		IList<BaseIndicatorValue> ComputeEMA(IList<Candle> candles, int periodCount);
		IList<BaseIndicatorValue> ComputeStochastic(IList<Candle> candles, int periodCount, int smaCountK, int smaCountD);
	}
}
