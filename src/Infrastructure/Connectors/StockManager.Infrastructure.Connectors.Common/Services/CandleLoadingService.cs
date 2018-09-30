using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public class CandleLoadingService
	{
		private readonly IRepository<Domain.Core.Entities.Market.Candle> _candleRepository;
		private readonly IMarketDataConnector _marketDataConnector;

		public CandleLoadingService(
			IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector)
		{
			_candleRepository = candleRepository;
			_marketDataConnector = marketDataConnector;
		}

		public async Task<IList<Candle>> LoadCandles(string currencyPairId,
			CandlePeriod period,
			int limit,
			DateTime currentMoment)
		{
			var momentsByPeriod = GetMomentsByPeriod(period, limit, currentMoment).ToList();
			momentsByPeriod.Sort();

			var storedCandles = _candleRepository.GetAll()
				.Where(entity => entity.CurrencyPair == currencyPairId && entity.Period == period &&
								 momentsByPeriod.Contains(entity.Moment))
				.OrderBy(entity => entity.Moment)
				.Select(entity => entity.ToModel())
				.ToList();

			if (momentsByPeriod.Count > storedCandles.Count)
			{
				var momentsToRequest = momentsByPeriod
					.Where(moment => storedCandles.All(candle => candle.Moment != moment))
					.ToList();
				var candlesLimit = momentsByPeriod.Count - momentsByPeriod.IndexOf(momentsToRequest.Min());
				var newCandles = await _marketDataConnector.GetCandles(currencyPairId, period, candlesLimit);

				var lastMoment = momentsToRequest.Last();
				if (newCandles.All(candle => candle.Moment != lastMoment))
				{
					var ticker = await _marketDataConnector.GetTicker(currencyPairId);
					newCandles.Add(new Candle
					{
						OpenPrice = ticker.LastPrice,
						ClosePrice = ticker.LastPrice,
						MinPrice = ticker.LastPrice,
						MaxPrice = ticker.LastPrice,
						VolumeInBaseCurrency = 0,
						VolumeInQuoteCurrency = ticker.LastPrice,
						Moment = lastMoment
					});
				}

				var candlesToInsert = newCandles
					.Where(candle => momentsToRequest.Contains(candle.Moment))
					.ToList();

				if (candlesToInsert.Any())
					_candleRepository.Insert(candlesToInsert
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

		private IEnumerable<DateTime> GetMomentsByPeriod(CandlePeriod period, int limit, DateTime currentMoment)
		{
			var lastDate = currentMoment;
			lastDate = new DateTime(lastDate.Year, lastDate.Month, lastDate.Day, lastDate.Hour, lastDate.Minute, 0);
			switch (period)
			{
				case CandlePeriod.Minute1:
					lastDate = lastDate.AddMinutes(-1);
					break;
				case CandlePeriod.Minute3:
					while (lastDate.Minute % 3 != 0)
						lastDate = lastDate.AddMinutes(-1);
					break;
				case CandlePeriod.Minute5:
					while (lastDate.Minute % 5 != 0)
						lastDate = lastDate.AddMinutes(-1);
					break;
				case CandlePeriod.Minute15:
					while (lastDate.Minute % 15 != 0)
						lastDate = lastDate.AddMinutes(-1);
					break;
				case CandlePeriod.Minute30:
					while (lastDate.Minute % 30 != 0)
						lastDate = lastDate.AddMinutes(-1);
					break;
				case CandlePeriod.Hour1:
					lastDate = new DateTime(lastDate.Year, lastDate.Month, lastDate.Day, lastDate.Hour, 0, 0);
					break;
				case CandlePeriod.Hour4:
					lastDate = new DateTime(lastDate.Year, lastDate.Month, lastDate.Day, lastDate.Hour, 0, 0);
					while (lastDate.Hour % 4 != 0)
						lastDate = lastDate.AddHours(-1);
					break;
				case CandlePeriod.Day1:
					lastDate = new DateTime(lastDate.Year, lastDate.Month, lastDate.Day, 0, 0, 0);
					break;
				case CandlePeriod.Day7:
					lastDate = new DateTime(lastDate.Year, lastDate.Month, lastDate.Day, 0, 0, 0);
					while (lastDate.DayOfWeek != DayOfWeek.Monday)
						lastDate = lastDate.AddDays(-1);
					break;
				case CandlePeriod.Month1:
					lastDate = new DateTime(lastDate.Year, lastDate.Month, 1, 0, 0, 0);
					break;
			}

			while (limit > 0)
			{
				yield return lastDate;
				switch (period)
				{
					case CandlePeriod.Minute1:
						lastDate = lastDate.AddMinutes(-1);
						break;
					case CandlePeriod.Minute3:
						lastDate = lastDate.AddMinutes(-3);
						break;
					case CandlePeriod.Minute5:
						lastDate = lastDate.AddMinutes(-5);
						break;
					case CandlePeriod.Minute15:
						lastDate = lastDate.AddMinutes(-15);
						break;
					case CandlePeriod.Minute30:
						lastDate = lastDate.AddMinutes(-30);
						break;
					case CandlePeriod.Hour1:
						lastDate = lastDate.AddHours(-1);
						break;
					case CandlePeriod.Hour4:
						lastDate = lastDate.AddHours(-4);
						break;
					case CandlePeriod.Day1:
						lastDate = lastDate.AddDays(-1);
						break;
					case CandlePeriod.Day7:
						lastDate = lastDate.AddDays(-7);
						break;
					case CandlePeriod.Month1:
						lastDate = lastDate.AddMonths(-1);
						break;
				}
				limit--;
			}
		}
	}
}
