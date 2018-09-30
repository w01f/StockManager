using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Orders
{
	public interface IOrdersService
	{
		Task SyncOrders();
		Task<IList<OrderPair>> GetActiveOrders();
		Task OpenPosition(NewOrderPositionInfo positionInfo);
		Task UpdatePosition(OrderPair orderPair);
		Task CancelPosition(OrderPair orderPair);
	}
}
