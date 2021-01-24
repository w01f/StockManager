using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition
{
	public interface IMarketOpenPositionAnalysisService
	{
		OpenPositionInfo ProcessMarketPosition(TradingPosition activeTradingPosition);
	}
}
