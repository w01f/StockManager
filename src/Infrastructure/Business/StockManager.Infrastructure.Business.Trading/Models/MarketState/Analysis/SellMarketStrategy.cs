using System.Threading.Tasks;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis
{
	class SellMarketStrategy
	{
		public async Task<ConditionCheckingResult> CheckConditions(
			TradingSettings settings,
			IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService
		)
		{
			var conditionCheckingResult = new ConditionCheckingResult();
			conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
			return conditionCheckingResult;
		}
	}
}
