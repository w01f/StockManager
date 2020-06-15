using System.Collections.Generic;
using System.Threading.Tasks;
using Ninject;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Dashboard.Controllers
{
	public class MainController
	{
		private readonly IStockRestConnector _stockRestConnector;

		[Inject]
		public MainController(IStockRestConnector stockRestConnector)
		{
			_stockRestConnector = stockRestConnector;
		}

		public async Task<IList<CurrencyPair>> GetCurrencyPairs()
		{
			return await _stockRestConnector.GetCurrencyPairs();
		}
	}
}
