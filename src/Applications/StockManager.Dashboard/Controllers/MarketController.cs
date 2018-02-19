using System.Collections.Generic;
using System.Threading.Tasks;
using Ninject;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Infrastructure.Business.Common.Services;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Dashboard.Controllers
{
	public class MarketController
	{
		private readonly MarketDataService _marketDataConnector;

		[Inject]
		public MarketController(MarketDataService marketDataConnector)
		{
			_marketDataConnector = marketDataConnector;
		}

		public async Task<IList<CurrencyPair>> GetCurrencyPairs()
		{
			return await _marketDataConnector.GetCurrensyPairs();
		}

		public async Task<IList<Candle>> GetCandles(string currencyPairId, CandlePeriod period, int limit = 100)
		{
			return await _marketDataConnector.GetCandles(currencyPairId, period, limit);
		}
	}
}
