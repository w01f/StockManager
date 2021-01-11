using Tinkoff.Trading.OpenApi.Models;

namespace StockManager.Infrastructure.Connectors.Socket.Models.Tinkoff.Market
{
	static class CurrencyPairMap
	{
		public static Infrastructure.Common.Models.Market.CurrencyPair ToOuterModel(this MarketInstrument source)
		{
			var target = new Infrastructure.Common.Models.Market.CurrencyPair
			{
				Id = source.Figi,
				BaseCurrencyId = source.Ticker,
				QuoteCurrencyId = source.Currency.ToString(),
				QuantityIncrement = source.MinPriceIncrement,
				TickSize = source.Lot,
				FeeCurrencyId = source.Currency.ToString()
			};

			return target;
		}
	}
}
