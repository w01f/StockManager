using System.Threading;
using System.Threading.Tasks;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers
{
	public interface ITradingController
	{
		Task StartTrading(CancellationToken cancellationToken);
	}
}
