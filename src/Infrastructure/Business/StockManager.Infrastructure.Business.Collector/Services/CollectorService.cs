using System;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Collector.Services
{
	public class CollectorService
	{
		private readonly CandleLoadingService _candleLoadingService;

		public CollectorService(CandleLoadingService candleLoadingService)
		{
			_candleLoadingService = candleLoadingService;
		}

		public async Task LoadMarketData(string currencyPairId, int candleLimit)
		{
			foreach (var candlePeriod in Enum.GetValues(typeof(CandlePeriod)).Cast<CandlePeriod>())
			{
				await _candleLoadingService.LoadCandles(currencyPairId,
					candlePeriod,
					candleLimit,
					DateTime.UtcNow);
			}
		}
	}
}
