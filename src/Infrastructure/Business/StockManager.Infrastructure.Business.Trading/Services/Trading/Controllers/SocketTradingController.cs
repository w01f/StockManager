using System;
using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers
{
	class SocketTradingController: ITradingController
	{
		public Task StartTrading()
		{
			throw new NotImplementedException();
		}

		private void AnalyzeActivePosition(OrderPair orderPair)
		{

		}

		private void AnalyzeNewPosition(CurrencyPair currencyPair)
		{

		}
	}
}
