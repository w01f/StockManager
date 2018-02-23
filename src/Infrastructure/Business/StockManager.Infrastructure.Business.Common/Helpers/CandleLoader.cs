using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Common.Helpers
{
	static class CandleLoader
	{
		public static async Task<IEnumerable<Candle>> Load(string currencyPairId,
			CandlePeriod period,
			int limit,
			IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector)
		{
			var momentsByPeriod = Extensions.GetMomentsByPeriod(period, limit).ToList();

			var storedCandles = candleRepository.GetAll()
				.Where(entity => entity.Period == period && momentsByPeriod.Contains(entity.Moment))
				.OrderBy(entity => entity.Moment)
				.Select(entity => entity.ToModel())
				.ToList();

			var requestLimit = momentsByPeriod.Count - storedCandles.Count;

			if (requestLimit > 0)
			{
				var newCandles = await marketDataConnector.GetCandles(currencyPairId, period, requestLimit);

				candleRepository.Insert(newCandles.Select(candle => candle.ToEntity(currencyPairId, period)));

				var candlesUnion = storedCandles
					.Union(newCandles)
					.OrderBy(candle => candle.Moment)
					.ToList();

				return candlesUnion;
			}

			return storedCandles;
		}
	}
}
