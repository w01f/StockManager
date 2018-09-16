using System.Threading.Tasks;
using StockManager.Domain.Core.Entities.Market;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition
{
	public abstract class BaseNewPositionAnalysisService
	{
		protected IRepository<Candle> CandleRepository { get; set; }
		protected IMarketDataConnector MarketDataConnector { get; set; }
		protected IIndicatorComputingService IndicatorComputingService { get; set; }
		protected ConfigurationService ConfigurationService { get; set; }

		protected abstract Task<ConditionCheckingResult> CheckConditions();
	}
}
