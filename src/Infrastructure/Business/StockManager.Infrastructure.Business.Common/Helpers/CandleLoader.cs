using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Common.Helpers
{
	public static class CandleLoader
	{
		public static async Task<IList<Candle>> Load(string currencyPairId,
			CandlePeriod period,
			int limit,
			DateTime currentMoment,
			IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector)
		{
			var momentsByPeriod = Extensions.GetMomentsByPeriod(period, limit, currentMoment).ToList();
			momentsByPeriod.Sort();

			var storedCandles = candleRepository.GetAll()
				.Where(entity => entity.CurrencyPair == currencyPairId && entity.Period == period && momentsByPeriod.Contains(entity.Moment))
				.OrderBy(entity => entity.Moment)
				.Select(entity => entity.ToModel())
				.ToList();

			if (momentsByPeriod.Count > storedCandles.Count)
			{
				var momentsToRequest = momentsByPeriod
					.Where(moment => storedCandles.All(candle => candle.Moment != moment))
					.ToList();
				var candlesLimit = momentsByPeriod.Count - momentsByPeriod.IndexOf(momentsToRequest.Min());
				var newCandles = await marketDataConnector.GetCandles(currencyPairId, period, candlesLimit);

				var candlesToInsert = newCandles
					.Where(candle => momentsToRequest.Contains(candle.Moment))
					.ToList();

				if (candlesToInsert.Any())
					candleRepository.Insert(candlesToInsert
						.Select(candle => candle.ToEntity(currencyPairId, period))
						.ToList());

				var candlesUnion = storedCandles
					.Union(candlesToInsert)
					.OrderBy(candle => candle.Moment)
					.ToList();

				return candlesUnion;
			}

			return storedCandles;
		}
	}
}
