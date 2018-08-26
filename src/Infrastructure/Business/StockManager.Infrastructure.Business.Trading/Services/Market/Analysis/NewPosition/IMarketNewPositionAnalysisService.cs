using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using System.Threading.Tasks;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition
{
	public interface IMarketNewPositionAnalysisService
	{
		Task<NewPositionInfo> ProcessMarketPosition(TradingSettings settings);
	}
}
