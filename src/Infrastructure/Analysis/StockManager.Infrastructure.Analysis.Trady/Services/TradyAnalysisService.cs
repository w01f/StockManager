using System.Collections.Generic;
using System.Linq;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Analysis.Trady.Models;
using StockManager.Infrastructure.Common.Models.Analysis;
using Trady.Analysis.Indicator;

namespace StockManager.Infrastructure.Analysis.Trady.Services
{
	public class TradyAnalysisService : IAnalysisService
	{
		public IList<IndicatorValue> ComputeEMA(IList<Infrastructure.Common.Models.Market.Candle> candles, int periodCount)
		{
			var emaCalcualtor = new ExponentialMovingAverage(candles.Select(candle => candle.ToInnerModel()), periodCount);

			var innerValues = emaCalcualtor.Compute();

			var outputValues = innerValues
				.Where(value => value.DateTime.HasValue)
				.Select(value => value.ToOuterModel())
				.ToList();

			return outputValues;
		}
	}
}
