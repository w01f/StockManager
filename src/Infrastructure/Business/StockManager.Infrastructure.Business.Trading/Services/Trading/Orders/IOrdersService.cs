using System.Threading.Tasks;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Orders
{
	public interface IOrdersService
	{
		Task<Order> CreateBuyLimitOrder(Order order);
		Task<Order> CreateSellLimitOrder(Order order);
		Task<Order> CreateSellMarketOrder(Order order);
		Task<Order> CancelOrder(Order order);
	}
}
