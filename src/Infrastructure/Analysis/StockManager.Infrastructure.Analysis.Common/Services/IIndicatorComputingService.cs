using System.Collections.Generic;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Analysis.Common.Services
{
	public interface IIndicatorComputingService
	{
		IList<BaseIndicatorValue> ComputeMACD(IList<Candle> candles, int emaPeriod1, int emaPeriod2, int signalPeriod);
		IList<BaseIndicatorValue> ComputeEMA(IList<Candle> candles, int period);
		IList<BaseIndicatorValue> ComputeStochastic(IList<Candle> candles, int basePeriod, int smaPeriodK, int smaPeriodD);
		IList<BaseIndicatorValue> ComputeRelativeStrengthIndex(IList<Candle> candles, int period);
		IList<BaseIndicatorValue> ComputeWilliamsR(IList<Candle> candles, int period);
		IList<BaseIndicatorValue> ComputeAccumulationDistribution(IList<Candle> candles);
		IList<BaseIndicatorValue> ComputeParabolicSAR(IList<Candle> candles);
	}
}
