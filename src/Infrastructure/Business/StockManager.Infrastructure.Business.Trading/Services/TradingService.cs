using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.Models.Trading;

namespace StockManager.Infrastructure.Business.Trading.Services
{
	public class TradingService
	{
		private readonly MarketStateService _marketStateService;

		public TradingService(MarketStateService marketStateService)
		{
			_marketStateService = marketStateService;
		}

		public async Task RunTradingIteration(TradingSettings settings)
		{
			var marketSate = await _marketStateService.EvaluateMarketState(settings);
		}
	}
}
