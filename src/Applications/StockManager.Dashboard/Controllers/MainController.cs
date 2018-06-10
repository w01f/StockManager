using System.Collections.Generic;
using System.Threading.Tasks;
using Ninject;
using StockManager.Infrastructure.Connectors.Common.Models;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Dashboard.Controllers
{
	public class MainController
	{
		private readonly IMarketDataConnector _marketDataConnector;

		[Inject]
		public MainController(IMarketDataConnector marketDataConnector)
		{
			_marketDataConnector = marketDataConnector;
		}

		public async Task<IList<CurrencyPair>> GetCurrencyPairs()
		{
			return await _marketDataConnector.GetCurrensyPairs();
		}
	}
}
