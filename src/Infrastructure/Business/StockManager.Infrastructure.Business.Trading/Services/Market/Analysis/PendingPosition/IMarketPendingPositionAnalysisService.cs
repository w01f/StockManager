using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition
{
	public interface IMarketPendingPositionAnalysisService
	{
		PendingPositionInfo ProcessMarketPosition(TradingPosition activeTradingPosition);
	}
}
