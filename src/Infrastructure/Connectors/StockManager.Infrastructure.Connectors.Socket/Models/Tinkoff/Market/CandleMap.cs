using Tinkoff.Trading.OpenApi.Models;

namespace StockManager.Infrastructure.Connectors.Socket.Models.Tinkoff.Market
{
	static class CandleMap
	{
		public static Infrastructure.Common.Models.Market.Candle ToOuterModel(this CandlePayload source)
		{
			var target = new Infrastructure.Common.Models.Market.Candle
			{
				Moment = source.Time,
				OpenPrice = source.Open,
				ClosePrice = source.Close,
				MaxPrice = source.High,
				MinPrice = source.Low,
				VolumeInBaseCurrency = source.Volume,
				VolumeInQuoteCurrency = source.Volume
			};
			return target;
		}
	}
}
