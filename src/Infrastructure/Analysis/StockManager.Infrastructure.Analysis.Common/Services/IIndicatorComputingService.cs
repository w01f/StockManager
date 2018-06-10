using System.Collections.Generic;
using StockManager.Infrastructure.Common.Models.Analysis;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Analysis.Common.Services
{
	public interface IIndicatorComputingService
	{
		IList<BaseIndicatorValue> ComputeMACD(IList<Candle> candles, int emaPeriod1, int emaPeriod2, int signalPeriod);
		IList<BaseIndicatorValue> ComputeEMA(IList<Candle> candles, int period);
		IList<BaseIndicatorValue> ComputeStochastic(IList<Candle> candles, int basePeriod, int smaPeriodK, int smaPeriodD);
	}
}
