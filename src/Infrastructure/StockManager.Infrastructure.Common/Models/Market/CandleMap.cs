using StockManager.Domain.Core.Enums;

namespace StockManager.Infrastructure.Common.Models.Market
{
	public static class CandleMap
	{
		public static Candle ToModel(this Domain.Core.Entities.Market.Candle source)
		{
			var target = new Candle();

			target.Moment = source.Moment;
			target.OpenPrice = source.OpenPrice;
			target.ClosePrice = source.ClosePrice;
			target.MaxPrice = source.MaxPrice;
			target.MinPrice = source.MinPrice;
			target.VolumeInBaseCurrency = source.VolumeInBaseCurrency;
			target.VolumeInQuoteCurrency = source.VolumeInQuoteCurrency;

			return target;
		}

		public static Domain.Core.Entities.Market.Candle ToEntity(this Candle source, string currencyPairId, CandlePeriod period, Domain.Core.Entities.Market.Candle target = null)
		{
			if (target == null)
				target = new Domain.Core.Entities.Market.Candle();

			target.CurrencyPair = currencyPairId;
			target.Period = period;
			target.Moment = source.Moment;
			target.OpenPrice = source.OpenPrice;
			target.ClosePrice = source.ClosePrice;
			target.MaxPrice = source.MaxPrice;
			target.MinPrice = source.MinPrice;
			target.VolumeInBaseCurrency = source.VolumeInBaseCurrency;
			target.VolumeInQuoteCurrency = source.VolumeInQuoteCurrency;

			return target;
		}
	}
}
