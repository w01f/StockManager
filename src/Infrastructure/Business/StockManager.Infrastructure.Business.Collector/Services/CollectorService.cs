using System;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Collector.Services
{
	public class CollectorService
	{
		private readonly IRepository<Domain.Core.Entities.Market.Candle> _candleRepository;
		private readonly IMarketDataConnector _marketDataConnector;

		public CollectorService(IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector)
		{
			_candleRepository = candleRepository;
			_marketDataConnector = marketDataConnector;
		}

		public async Task LoadMaketData(string currencyPairId, int candleLimit)
		{
			foreach (var candlePeriod in Enum.GetValues(typeof(CandlePeriod)).Cast<CandlePeriod>())
			{
				await CandleLoader.Load(currencyPairId,
					candlePeriod,
					candleLimit,
					DateTime.UtcNow, 
					_candleRepository,
					_marketDataConnector);
			}
		}
	}
}
