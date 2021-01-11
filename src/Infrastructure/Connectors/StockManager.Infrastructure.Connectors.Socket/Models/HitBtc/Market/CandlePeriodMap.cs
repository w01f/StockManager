using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Connectors.Common.Common;

namespace StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.Market
{
	public static class CandlePeriodMap
	{
		public static string ToInnerFormat(this CandlePeriod candlePeriod)
		{
			switch (candlePeriod)
			{
				case CandlePeriod.Minute1:
					return "M1";
				case CandlePeriod.Minute3:
					return "M3";
				case CandlePeriod.Minute5:
					return "M5";
				case CandlePeriod.Minute15:
					return "M15";
				case CandlePeriod.Minute30:
					return "M30";
				case CandlePeriod.Hour1:
					return "H1";
				case CandlePeriod.Hour4:
					return "H4";
				case CandlePeriod.Day1:
					return "D1";
				case CandlePeriod.Day7:
					return "D7";
				case CandlePeriod.Month1:
					return "1M";
				default:
					throw new ConnectorException("Undefined candle period", null);
			}
		}

		public static CandlePeriod ToOuterFormat(string candlePeriod)
		{
			switch (candlePeriod?.ToUpper())
			{
				case "M1":
					return CandlePeriod.Minute1;
				case "M3":
					return CandlePeriod.Minute3;
				case "M5":
					return CandlePeriod.Minute5;
				case "M15":
					return CandlePeriod.Minute15;
				case "M30":
					return CandlePeriod.Minute30;
				case "H1":
					return CandlePeriod.Hour1;
				case "H4":
					return CandlePeriod.Hour4;
				case "D1":
					return CandlePeriod.Day1;
				case "D7":
					return CandlePeriod.Day7;
				case "1M":
					return CandlePeriod.Month1;
				default:
					throw new ConnectorException("Undefined candle period", null);
			}
		}
	}
}
