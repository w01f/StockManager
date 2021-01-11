using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Connectors.Common.Common;
using Tinkoff.Trading.OpenApi.Models;

namespace StockManager.Infrastructure.Connectors.Socket.Models.Tinkoff.Market
{
	public static class CandlePeriodMap
	{
		public static CandleInterval ToInnerFormat(this CandlePeriod candlePeriod)
		{
			switch (candlePeriod)
			{
				case CandlePeriod.Minute1:
					return CandleInterval.Minute;
				case CandlePeriod.Minute3:
					return CandleInterval.ThreeMinutes;
				case CandlePeriod.Minute5:
					return CandleInterval.FiveMinutes;
				case CandlePeriod.Minute15:
					return CandleInterval.QuarterHour;
				case CandlePeriod.Minute30:
					return CandleInterval.HalfHour;
				case CandlePeriod.Hour1:
					return CandleInterval.Hour;
				case CandlePeriod.Day1:
					return CandleInterval.Day;
				case CandlePeriod.Day7:
					return CandleInterval.Week;
				case CandlePeriod.Month1:
					return CandleInterval.Month;
				default:
					throw new ConnectorException("Undefined candle period", null);
			}
		}

		public static CandlePeriod ToOuterFormat(CandleInterval candleInterval)
		{
			switch (candleInterval)
			{
				case CandleInterval.Minute:
					return CandlePeriod.Minute1;
				case CandleInterval.ThreeMinutes:
					return CandlePeriod.Minute3;
				case CandleInterval.FiveMinutes:
					return CandlePeriod.Minute5;
				case CandleInterval.QuarterHour:
					return CandlePeriod.Minute15;
				case CandleInterval.HalfHour:
					return CandlePeriod.Minute30;
				case CandleInterval.Hour:
					return CandlePeriod.Hour1;
				case CandleInterval.Day:
					return CandlePeriod.Day1;
				case CandleInterval.Week:
					return CandlePeriod.Day7;
				case CandleInterval.Month:
					return CandlePeriod.Month1;
				default:
					throw new ConnectorException("Undefined candle period", null);
			}
		}
	}
}
