using System.Collections.Generic;
using System.Linq;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Analysis.Trady.Models;
using StockManager.Infrastructure.Common.Models.Analysis;
using StockManager.Infrastructure.Common.Models.Market;
using Trady.Analysis.Indicator;

namespace StockManager.Infrastructure.Analysis.Trady.Services
{
	public class TradyIndicatorComputingService : IIndicatorComputingService
	{
		public IList<BaseIndicatorValue> ComputeMACD(IList<Candle> candles, int emaPeriod1, int emaPeriod2, int signalPeriod)
		{
			var indicator = new MovingAverageConvergenceDivergence(candles.Select(candle => candle.ToInnerModel()), emaPeriod1, emaPeriod2, signalPeriod);

			var innerValues = indicator.Compute();

			var outputValues = new List<BaseIndicatorValue>();

			outputValues.AddRange(innerValues
				.Where(value => value.DateTime.HasValue)
				.Select(value => value.ToMACDOuterModel()));

			return outputValues;
		}

		public IList<BaseIndicatorValue> ComputeEMA(IList<Candle> candles, int period)
		{
			var indicator = new ExponentialMovingAverage(candles.Select(candle => candle.ToInnerModel()), period);

			var innerValues = indicator.Compute();

			var outputValues = new List<BaseIndicatorValue>();

			outputValues.AddRange(innerValues
				.Where(value => value.DateTime.HasValue)
				.Select(value => value.ToOuterModel()));

			return outputValues;
		}

		public IList<BaseIndicatorValue> ComputeStochastic(IList<Candle> candles, int basePeriod, int smaPeriodK, int smaPeriodD)
		{
			var indicator = new Stochastics.Full(candles.Select(candle => candle.ToInnerModel()), basePeriod, smaPeriodK, smaPeriodD);

			var innerValues = indicator.Compute();

			var outputValues = new List<BaseIndicatorValue>();

			outputValues.AddRange(innerValues
				.Where(value => value.DateTime.HasValue)
				.Select(value => value.ToStochasticOuterModel()));

			return outputValues;
		}
	}
}
