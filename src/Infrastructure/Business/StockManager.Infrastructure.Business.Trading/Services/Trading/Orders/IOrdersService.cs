using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Orders
{
	public interface IOrdersService
	{
		Task SyncOrders(TradingSettings settings);
		Task<OrderPair> GetActiveOrder(string currencyPairId);
		Task OpenOrder(NewOrderPositionInfo positionInfo, TradingSettings settings);
		Task UpdateOrder(OrderPair orderPair, TradingSettings settings);
		Task CancelOrder(OrderPair orderPair);
	}
}
