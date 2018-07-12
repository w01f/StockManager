using System.Collections.Generic;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Trading;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.TrendIdentification
{
	class TrandIndentificationActiveState
	{
		public IList<Candle> Candles { get; }
		public TradingSettings Settings { get; }
		public IIndicatorComputingService IndicatorComputingService { get; }
		public Dictionary<IndicatorType, IList<BaseIndicatorValue>> IndicatorValueCache { get; }

		public TrandIndentificationActiveState(IList<Candle> candles, IIndicatorComputingService indicatorComputingService, TradingSettings settings)
		{
			IndicatorValueCache = new Dictionary<IndicatorType, IList<BaseIndicatorValue>>();

			Candles = new List<Candle>(candles);
			Settings = settings;
			IndicatorComputingService = indicatorComputingService;
		}
	}
}
