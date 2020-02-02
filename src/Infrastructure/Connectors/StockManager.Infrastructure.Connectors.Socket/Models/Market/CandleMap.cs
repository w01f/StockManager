namespace StockManager.Infrastructure.Connectors.Socket.Models.Market
{
	static class CandleMap
	{
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
