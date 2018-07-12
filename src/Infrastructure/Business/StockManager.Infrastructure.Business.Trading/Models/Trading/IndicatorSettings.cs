using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Trading
{
	public abstract class IndicatorSettings
	{
		public IndicatorType Type { get; set; }
	}

	public class CommonIndicatorSettings : IndicatorSettings
	{
		public int Period { get; set; }
	}

	public class MACDSettings : IndicatorSettings
	{
		public int EMAPeriod1 { get; set; }
		public int EMAPeriod2 { get; set; }
		public int SignalPeriod { get; set; }

		public MACDSettings()
		{
			Type = IndicatorType.MACD;
		}
	}

	public class StochasticSettings : IndicatorSettings
	{
		public int Period { get; set; }
		public int SMAPeriodK { get; set; }
		public int SMAPeriodD { get; set; }

		public StochasticSettings()
		{
			Type = IndicatorType.Stochastic;
		}
	}
}
