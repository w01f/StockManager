using System.Collections.Generic;
using System.Threading.Tasks;
using Ninject;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Dashboard.Controllers
{
	public class MainController
	{
		private readonly IMarketDataRestConnector _marketDataRestConnector;

		[Inject]
		public MainController(IMarketDataRestConnector marketDataRestConnector)
		{
			_marketDataRestConnector = marketDataRestConnector;
		}

		public async Task<IList<CurrencyPair>> GetCurrencyPairs()
		{
			return await _marketDataRestConnector.GetCurrensyPairs();
		}
	}
}
