using System;

namespace StockManager.Infrastructure.Connectors.HitBtc.Rest.Models.Market
{
	static class TickerMap
	{
		public static Infrastructure.Common.Models.Market.Ticker ToOuterModel(this Ticker source)
		{
			var target = new Infrastructure.Common.Models.Market.Ticker
			{
				CurrencyPairId = source.CurrencyPairId,
				BestAskPrice = source.BestAskPrice ?? 0,
				BestBidPrice = source.BestBidPrice ?? 0,
				LastPrice = source.LastPrice ?? 0,
				OpenPrice = source.OpenPrice ?? 0,
				MaxPrice = source.MaxPrice ?? 0,
				MinPrice = source.MinPrice ?? 0,
				VolumeInBaseCurrency = source.VolumeInBaseCurrency ?? 0,
				VolumeInQuoteCurrency = source.VolumeInQuoteCurrency ?? 0,
				Updated = source.Updated ?? DateTime.MinValue,
			};
			return target;
		}
	}
}
