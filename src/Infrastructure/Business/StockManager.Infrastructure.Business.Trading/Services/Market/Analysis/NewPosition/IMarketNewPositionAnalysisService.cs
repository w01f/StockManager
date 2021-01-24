using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition
{
	public interface IMarketNewPositionAnalysisService
	{
		NewPositionInfo ProcessMarketPosition(CurrencyPair currencyPair);
	}
}
