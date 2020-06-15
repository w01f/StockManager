using System;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers
{
	public interface ITradingController
	{
		event EventHandler<UnhandledExceptionEventArgs> Exception; 

		void StartTrading();
	}
}
