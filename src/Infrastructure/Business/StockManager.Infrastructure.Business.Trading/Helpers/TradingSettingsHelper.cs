using StockManager.Infrastructure.Business.Trading.Models.Trading;

namespace StockManager.Infrastructure.Business.Trading.Helpers
{
	public static class TradingSettingsHelper
	{
		public static TradingSettings InitializeFromTemplate(this TradingSettings target, TradingSettings template)
		{
			target.CurrentMoment = template.CurrentMoment;
			target.CurrencyPairId = template.CurrencyPairId;
			target.Period = template.Period;
			target.CandleRangeSize = template.CandleRangeSize;
			target.IndicatorSettings.AddRange(template.IndicatorSettings);
			return target;
		}
	}
}
