using StockManager.Domain.Core.Common.Enums;

namespace StockManager.Infrastructure.Common.Models.Market
{
	public static class CoreMapper
	{
		public static Candle ToModel(this Domain.Core.Entities.Market.Candle source)
		{
			var target = new Candle
			{
				Moment = source.Moment,
				OpenPrice = source.OpenPrice,
				ClosePrice = source.ClosePrice,
				MaxPrice = source.MaxPrice,
				MinPrice = source.MinPrice,
				VolumeInBaseCurrency = source.VolumeInBaseCurrency,
				VolumeInQuoteCurrency = source.VolumeInQuoteCurrency
			};
			return target;
		}

		public static Domain.Core.Entities.Market.Candle ToEntity(this Candle source, string currencyPairId, CandlePeriod period)
		{
			var target = new Domain.Core.Entities.Market.Candle
			{
				CurrencyPair = currencyPairId,
				Period = period,
				Moment = source.Moment,
				OpenPrice = source.OpenPrice,
				ClosePrice = source.ClosePrice,
				MaxPrice = source.MaxPrice,
				MinPrice = source.MinPrice,
				VolumeInBaseCurrency = source.VolumeInBaseCurrency,
				VolumeInQuoteCurrency = source.VolumeInQuoteCurrency
			};
			return target;
		}
	}
}
