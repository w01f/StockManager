using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition
{
	public interface IMarketOpenPositionAnalysisService
	{
		Task<OpenPositionInfo> ProcessMarketPosition(OrderPair activeOrderPair);
	}
}
