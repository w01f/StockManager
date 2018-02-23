using StockManager.Infrastructure.Common.Common;
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

		public static Infrastructure.Common.Models.Analysis.IndicatorValue ToOuterModel(this AnalyzableTick<decimal?> source)
		{
			if (!source.DateTime.HasValue)
				throw new AnalysisException("Indicator value should be assigned for the moment");
			var target = new Infrastructure.Common.Models.Analysis.IndicatorValue(source.DateTime.Value)
			{
				Value = source.Tick,
			};
			return target;
		}
	}
}
