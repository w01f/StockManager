using System.Threading.Tasks;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition
{
	public abstract class BaseNewPositionAnalysisService
	{
		protected CandleLoadingService CandleLoadingService { get; set; }
		protected OrderBookLoadingService OrderBookLoadingService { get; set; }
		protected IIndicatorComputingService IndicatorComputingService { get; set; }
		protected ConfigurationService ConfigurationService { get; set; }

		protected abstract Task<ConditionCheckingResult> CheckConditions(CurrencyPair currencyPair);
	}
}
