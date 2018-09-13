using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;

namespace StockManager.Infrastructure.Business.Trading.Helpers
{
	public static class TradingSettingsHelper
	{
		public static TradingSettings InitializeFromTemplate(this TradingSettings target, TradingSettings template)
		{
			target.Moment = template.Moment;
			target.CurrencyPairId = template.CurrencyPairId;
			target.Period = template.Period;
			target.CandleRangeSize = template.CandleRangeSize;
			return target;
		}

		public static CandlePeriod GetLowerFramePeriod(this CandlePeriod targetPeriod)
		{
			switch (targetPeriod)
			{
				case CandlePeriod.Month1:
					return CandlePeriod.Day7;
				case CandlePeriod.Day7:
					return CandlePeriod.Day1;
				case CandlePeriod.Day1:
					return CandlePeriod.Hour4;
				case CandlePeriod.Hour4:
					return CandlePeriod.Hour1;
				case CandlePeriod.Hour1:
					return CandlePeriod.Minute15;
				case CandlePeriod.Minute30:
					return CandlePeriod.Minute5;
				case CandlePeriod.Minute15:
					return CandlePeriod.Minute5;
				case CandlePeriod.Minute5:
					return CandlePeriod.Minute1;
				case CandlePeriod.Minute3:
					return CandlePeriod.Minute1;
				default:
					return targetPeriod;
			}
		}

		public static CandlePeriod GetHigherFramePeriod(this CandlePeriod targetPeriod)
		{
			switch (targetPeriod)
			{
				case CandlePeriod.Day7:
					return CandlePeriod.Month1;
				case CandlePeriod.Day1:
					return CandlePeriod.Day7;
				case CandlePeriod.Hour4:
					return CandlePeriod.Day1;
				case CandlePeriod.Hour1:
					return CandlePeriod.Hour4;
				case CandlePeriod.Minute30:
					return CandlePeriod.Hour4;
				case CandlePeriod.Minute15:
					return CandlePeriod.Hour1;
				case CandlePeriod.Minute5:
					return CandlePeriod.Minute30;
				case CandlePeriod.Minute3:
					return CandlePeriod.Minute15;
				case CandlePeriod.Minute1:
					return CandlePeriod.Minute5;
				default:
					return targetPeriod;
			}
		}
	}
}
