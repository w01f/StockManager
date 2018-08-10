using System.Threading.Tasks;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis;
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

		public async Task<MarketInfo> EstimateBuyOption(TradingSettings settings)
		{
			var marketInfo = new MarketInfo { MarketSignal = MarketSignalType.Hold };

			var bullishTrendIdentificationStrategy = new TripleFrameLongMarketStrategy();
			var conditionCheckingResult = await bullishTrendIdentificationStrategy.CheckConditions(
				settings,
				_candleRepository,
				_marketDataConnector,
				_indicatorComputingService
				);
			if (conditionCheckingResult.ResultType == ConditionCheckingResultType.Passed)
				marketInfo = new MarketInfo { MarketSignal = MarketSignalType.Buy };

			return marketInfo;
		}

		public async Task<MarketInfo> EstimateSellOption(TradingSettings settings)
		{
			var marketInfo = new MarketInfo { MarketSignal = MarketSignalType.Hold };

			//var bearishTrendIdentificationStrategy = new SellMarketStrategy();
			//var conditionCheckingResult = await bearishTrendIdentificationStrategy.CheckConditions(
			//	settings,
			//	_candleRepository,
			//	_marketDataConnector,
			//	_indicatorComputingService
			//);
			//if (conditionCheckingResult.ResultType == ConditionCheckingResultType.Passed)
			//	marketInfo = new MarketInfo { MarketSignal = MarketSignalType.Sell };

			return marketInfo;
		}
	}
}
