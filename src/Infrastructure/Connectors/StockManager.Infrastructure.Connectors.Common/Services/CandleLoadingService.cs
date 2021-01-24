using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Common;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public class CandleLoadingService
	{
		private static readonly object DbOperationLocker = new object();

		private readonly IRepository<Domain.Core.Entities.Market.Candle> _candleRepository;
		private readonly IStockSocketConnector _stockSocketConnector;

		private readonly IList<Tuple<string, CandlePeriod>> _existingSubscriptions = new List<Tuple<string, CandlePeriod>>();

		public event EventHandler<CandlesUpdatedEventArgs> CandlesUpdated;

		public CandleLoadingService(
			IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IStockSocketConnector stockSocketConnector)
		{
			_candleRepository = candleRepository ?? throw new ArgumentNullException(nameof(candleRepository));
			_stockSocketConnector = stockSocketConnector ?? throw new ArgumentNullException(nameof(stockSocketConnector));
		}

		public async Task InitSubscription(string currencyPairId, IList<CandlePeriod> candlePeriods)
		{
			foreach (var candlePeriod in candlePeriods)
			{
				if (_existingSubscriptions.Any(item => item.Item1 == currencyPairId && item.Item2 == candlePeriod))
					return;

				await _stockSocketConnector.SubscribeOnCandles(currencyPairId, candlePeriod, receivedCandles =>
				{
					UpdateCandles(currencyPairId, candlePeriod, receivedCandles);
					OnCandlesUpdated(currencyPairId, candlePeriod);
				});

				_existingSubscriptions.Add(Tuple.Create(currencyPairId, candlePeriod));
			}
		}

		public IList<Candle> LoadCandles(string currencyPairId,
			CandlePeriod period,
			int limit,
			DateTime currentMoment)
		{
			var momentsByPeriod = GetMomentsByPeriod(period, limit, currentMoment).ToList();
			momentsByPeriod.Sort();

			IList<Candle> storedCandles;
			lock (DbOperationLocker)
			{
				storedCandles = _candleRepository.GetAll()
					.Where(entity => entity.CurrencyPair == currencyPairId && momentsByPeriod.Contains(entity.Moment))
					.Select(entity => Tuple.Create(entity.ToModel(), entity.Period))
					.ToList()
					.GroupBy(candle => candle.Item1.Moment)
					.Select(candleGroup =>
					{
						var candle = candleGroup.FirstOrDefault(tuple => tuple.Item2 == period) ?? candleGroup.First();
						return candle.Item1;
					})
					.OrderBy(candle => candle.Moment)
					.ToList();
			}

			return storedCandles;
		}

		public void UpdateCandles(string currencyPairId, CandlePeriod candlePeriod, IList<Candle> candles)
		{
			lock (DbOperationLocker)
			{
				var storedEntities = _candleRepository.GetAll()
					.Where(entity => entity.CurrencyPair == currencyPairId && entity.Period == candlePeriod)
					.OrderByDescending(entity => entity.Moment)
					.Take(candles.Count)
					.ToList();

				var storedCandles = storedEntities
					.Select(entity => entity.ToModel())
					.ToList();

				var newCandles = new List<Candle>();
				var updatedCandles = new List<Candle>();
				foreach (var receivedCandle in candles)
				{
					var storedCandle = storedCandles.FirstOrDefault(candle => candle.Moment == receivedCandle.Moment);
					if (storedCandle == null)
						newCandles.Add(receivedCandle);
					else
					{
						storedCandle.OpenPrice = receivedCandle.OpenPrice;
						storedCandle.ClosePrice = receivedCandle.ClosePrice;
						storedCandle.MaxPrice = receivedCandle.MaxPrice;
						storedCandle.MinPrice = receivedCandle.MinPrice;
						storedCandle.VolumeInBaseCurrency = receivedCandle.VolumeInBaseCurrency;
						storedCandle.VolumeInQuoteCurrency = receivedCandle.VolumeInQuoteCurrency;
						updatedCandles.Add(storedCandle);
					}
				}

				if (newCandles.Any())
					_candleRepository.Insert(newCandles
						.Select(candle => candle.ToEntity(currencyPairId, candlePeriod))
						.ToList());

				if (updatedCandles.Any())
					_candleRepository.Update(updatedCandles
						.Select(candle =>
						{
							var storedEntity = storedEntities.Single(entity => entity.Moment == candle.Moment);
							return candle.ToEntity(currencyPairId, candlePeriod, storedEntity);
						})
						.ToList());
			}
		}

		private static IEnumerable<DateTime> GetMomentsByPeriod(CandlePeriod period, int limit, DateTime currentMoment)
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

		private void OnCandlesUpdated(string currencyPairId, CandlePeriod period)
		{
			CandlesUpdated?.Invoke(this, new CandlesUpdatedEventArgs(currencyPairId, period));
		}
	}
}
