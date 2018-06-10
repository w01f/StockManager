namespace StockManager.Infrastructure.Connectors.HitBtc.Rest.Models.Market
{
	static class BusinessMapper
	{
		public static Common.Models.CurrencyPair ToOuterModel(this CurrencyPair source)
		{
			var target = new Common.Models.CurrencyPair
			{
				Id = source.Id,
				BaseCurrencyId = source.BaseCurrencyId,
				QuoteCurrencyId = source.QuoteCurrencyId,
				QuantityIncrement = source.QuantityIncrement,
				TickSize = source.TickSize,
				TakeLiquidityRate = source.TakeLiquidityRate,
				ProvideLiquidityRate = source.ProvideLiquidityRate,
				FeeCurrencyId = source.FeeCurrencyId
			};
			return target;
		}

		public static Infrastructure.Common.Models.Market.Candle ToOuterModel(this Candle source)
		{
			var target = new Infrastructure.Common.Models.Market.Candle
			{
				Moment = source.Timestamp,
				OpenPrice = source.Open,
				ClosePrice = source.Close,
				MaxPrice = source.Max,
				MinPrice = source.Min,
				VolumeInBaseCurrency = source.Volume,
				VolumeInQuoteCurrency = source.VolumeQuote
			};
			return target;
		}
	}
}
