using System;
using System.Collections.Generic;
using System.Linq;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Analysis.Trady.Models;
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

		public IList<BaseIndicatorValue> ComputeRelativeStrengthIndex(IList<Candle> candles, Int32 period)
		{
			var indicator = new RelativeStrengthIndex(candles.Select(candle => candle.ToInnerModel()), period);
			var innerValues = indicator.Compute();

			var outputValues = new List<BaseIndicatorValue>();

			outputValues.AddRange(innerValues
				.Where(value => value.DateTime.HasValue)
				.Select(value => value.ToOuterModel()));

			return outputValues;
		}

		public IList<BaseIndicatorValue> ComputeWilliamsR(IList<Candle> candles, int period)
		{
			var outputValues = new List<BaseIndicatorValue>();

			for (var i = 0; i < candles.Count; i++)
			{
				if (i < period)
					outputValues.Add(new SimpleIndicatorValue(candles[i].Moment));
				else
				{
					var valuableCandles = candles.Skip(i + 1 - period).Take(period).ToList();
					var maxPrice = valuableCandles.Max(candle => candle.MaxPrice);
					var minPrice = valuableCandles.Min(candle => candle.MinPrice);

					outputValues.Add(new SimpleIndicatorValue(candles[i].Moment)
					{
						Value = 100 * ((maxPrice - candles[i].ClosePrice) / (maxPrice - minPrice))
					});
				}
			}

			return outputValues;
		}

		public IList<BaseIndicatorValue> ComputeAccumulationDistribution(IList<Candle> candles)
		{
			var indicator = new AccumulationDistributionLine(candles.Select(candle => candle.ToInnerModel()));
			var innerValues = indicator.Compute();

			var outputValues = new List<BaseIndicatorValue>();

			outputValues.AddRange(innerValues
				.Where(value => value.DateTime.HasValue)
				.Select(value => value.ToOuterModel()));

			return outputValues;
		}

		public IList<BaseIndicatorValue> ComputeParabolicSAR(IList<Candle> candles)
		{
			var indicator = new ParabolicStopAndReverse(candles.Select(candle => candle.ToInnerModel()));
			var innerValues = indicator.Compute();

			var outputValues = new List<BaseIndicatorValue>();

			outputValues.AddRange(innerValues
				.Where(value => value.DateTime.HasValue)
				.Select(value => value.ToOuterModel()));

			return outputValues;
		}
	}
}
