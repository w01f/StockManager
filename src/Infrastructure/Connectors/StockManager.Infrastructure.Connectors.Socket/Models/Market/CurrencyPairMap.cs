namespace StockManager.Infrastructure.Connectors.Socket.Models.Market
{
	static class CurrencyPairMap
	{
		public static Infrastructure.Common.Models.Market.CurrencyPair ToOuterModel(this CurrencyPair source)
		{
			var target = new Infrastructure.Common.Models.Market.CurrencyPair
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
	}
}
