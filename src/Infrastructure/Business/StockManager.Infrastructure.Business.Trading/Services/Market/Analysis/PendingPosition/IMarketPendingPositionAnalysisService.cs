using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition
{
	public interface IMarketPendingPositionAnalysisService
	{
		Task<PendingPositionInfo> ProcessMarketPosition(TradingPosition activeTradingPosition);
	}
}
