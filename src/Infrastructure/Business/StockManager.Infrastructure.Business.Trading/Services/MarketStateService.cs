using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.MarketState.Info;
using StockManager.Infrastructure.Business.Trading.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Services
{
	public class MarketStateService
	{
		private readonly IRepository<Domain.Core.Entities.Market.Candle> _candleRepository;
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly IIndicatorComputingService _indicatorComputingService;

		public MarketStateService(IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService)
		{
			_candleRepository = candleRepository;
			_marketDataConnector = marketDataConnector;
			_indicatorComputingService = indicatorComputingService;
		}

		public async Task<BaseMarketStateInfo> EvaluateMarketState(TradingSettings settings)
		{
			var candles = (await CandleLoader.Load(
				settings.CurrencyPairId,
				settings.Period,
				settings.CandleRangeSize,
				settings.CurrentMoment,
				_candleRepository,
				_marketDataConnector))
				.ToList();

			BaseMarketStateInfo marketSateInfo = null;

			var trendingType = TrendIdentificationHelper.IdentifyMarketTrend(candles, _indicatorComputingService, settings);
			switch (trendingType)
			{
				case MarketTrendType.Bullish:
					var bullishMarketInfo = new BullishMarketInfo();
					await bullishMarketInfo.ObtainTradingData();
					marketSateInfo = bullishMarketInfo;
					break;
				case MarketTrendType.Bearish:
					var bearishMarketInfo = new BearishMarketInfo();
					await bearishMarketInfo.ObtainTradingData();
					marketSateInfo = bearishMarketInfo;
					break;
				case MarketTrendType.Accumulation:
					marketSateInfo = new AccumulationMarketInfo();
					break;
			}

			return marketSateInfo;
		}
	}
}
