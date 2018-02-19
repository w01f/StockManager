using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Common.Services
{
	public class MarketDataService
	{
		private readonly IRepository<Domain.Core.Entities.Market.Candle> _candleRepository;

		private readonly IMarketDataConnector _marketDataConnector;

		public MarketDataService(IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector)
		{
			_candleRepository = candleRepository;
			_marketDataConnector = marketDataConnector;
		}

		public async Task<IList<CurrencyPair>> GetCurrensyPairs()
		{
			return await _marketDataConnector.GetCurrensyPairs();
		}

		public async Task<IList<Candle>> GetCandles(string currencyPairId, CandlePeriod period, int limit)
		{
			var momentsByPeriod = Extensions.GetMomentsByPeriod(period, limit).ToList();

			var storedCandles = _candleRepository.GetAll()
				.Where(entity => entity.Period == period && momentsByPeriod.Contains(entity.Moment))
				.OrderByDescending(entity => entity.Moment)
				.Select(entity => entity.ToModel())
				.ToList();

			var requestLimit = momentsByPeriod.Count - storedCandles.Count;

			if (requestLimit > 0)
			{
				var newCandles = await _marketDataConnector.GetCandles(currencyPairId, period, requestLimit);

				_candleRepository.Insert(newCandles.Select(candle => candle.ToEntity(currencyPairId, period)));

				var candlesUnion = storedCandles
					.Union(newCandles)
					.OrderByDescending(candle => candle.Moment)
					.ToList();

				return candlesUnion;
			}

			return storedCandles;
		}
	}
}
