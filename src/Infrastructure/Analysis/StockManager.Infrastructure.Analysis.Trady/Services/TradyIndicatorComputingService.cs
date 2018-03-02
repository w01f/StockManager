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
		public IList<BaseIndicatorValue> ComputeEMA(IList<Candle> candles, int periodCount)
		{
			var indicator = new ExponentialMovingAverage(candles.Select(candle => candle.ToInnerModel()), periodCount);

			var innerValues = indicator.Compute();

			var outputValues = new List<BaseIndicatorValue>();

			outputValues.AddRange(innerValues
				.Where(value => value.DateTime.HasValue)
				.Select(value => value.ToOuterModel()));

			return outputValues;
		}

		public IList<BaseIndicatorValue> ComputeStochastic(IList<Candle> candles, int periodCount, int smaCountK, int smaCountD)
		{
			var indicator = new Stochastics.Full(candles.Select(candle => candle.ToInnerModel()), periodCount, smaCountK, smaCountD);

			var innerValues = indicator.Compute();

			var outputValues = new List<BaseIndicatorValue>();

			outputValues.AddRange(innerValues
				.Where(value => value.DateTime.HasValue)
				.Select(value => value.ToOuterModel()));

			return outputValues;
		}
	}
}
