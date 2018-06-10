using StockManager.Infrastructure.Analysis.Common.Common;
using StockManager.Infrastructure.Analysis.Common.Models;
using Trady.Analysis;
using Trady.Core;

namespace StockManager.Infrastructure.Analysis.Trady.Models
{
	static class BusinessMapper
	{
		public static Candle ToInnerModel(this Infrastructure.Common.Models.Market.Candle source)
		{
			return new Candle(source.Moment, source.OpenPrice, source.MaxPrice, source.MinPrice, source.ClosePrice,
				source.VolumeInBaseCurrency);
		}

		public static SimpleIndicatorValue ToOuterModel(this AnalyzableTick<decimal?> source)
		{
			if (!source.DateTime.HasValue)
				throw new AnalysisException("Indicator value should be assigned for the moment");
			var target = new SimpleIndicatorValue(source.DateTime.Value)
			{
				Value = source.Tick,
			};
			return target;
		}

		public static StochasticValue ToStochasticOuterModel(this AnalyzableTick<(decimal? K, decimal? D, decimal? J)> source)
		{
			if (!source.DateTime.HasValue)
				throw new AnalysisException("Indicator value should be assigned for the moment");
			var target = new StochasticValue(source.DateTime.Value)
			{
				K = source.Tick.K,
				D = source.Tick.D,
				J = source.Tick.J,
			};
			return target;
		}

		public static MACDValue ToMACDOuterModel(this AnalyzableTick<(decimal? MacdLine, decimal? SignalLine, decimal? MacdHistogram)> source)
		{
			if (!source.DateTime.HasValue)
				throw new AnalysisException("Indicator value should be assigned for the moment");
			var target = new MACDValue(source.DateTime.Value)
			{
				MACD = source.Tick.MacdLine,
				Signal = source.Tick.SignalLine,
				Histogram = source.Tick.MacdHistogram,
			};
			return target;
		}
	}
}
