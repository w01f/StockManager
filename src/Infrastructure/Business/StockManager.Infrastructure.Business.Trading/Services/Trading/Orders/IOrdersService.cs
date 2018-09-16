using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Orders
{
	public interface IOrdersService
	{
		Task SyncOrders();
		Task<OrderPair> GetActiveOrder(string currencyPairId);
		Task OpenOrder(NewOrderPositionInfo positionInfo);
		Task UpdateOrder(OrderPair orderPair);
		Task CancelOrder(OrderPair orderPair);
	}
}
