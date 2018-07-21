using System.Linq;
using StockManager.Infrastructure.Business.Trading.Common.Enums;

namespace StockManager.Infrastructure.Business.Trading.Models.Trading
{
	public abstract class IndicatorSettings
	{
		public const int DeviationSize = 5;

		public IndicatorType Type { get; set; }
		public abstract int RequiredCandleRangeSize { get; }
	}

	public class CommonIndicatorSettings : IndicatorSettings
	{
		public int Period { get; set; }

		public override int RequiredCandleRangeSize => Period + DeviationSize + 1;
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

		public override int RequiredCandleRangeSize => new[] { EMAPeriod1, EMAPeriod2, SignalPeriod }.Max() + DeviationSize + 1;
	}

	public class StochasticSettings : IndicatorSettings
	{
		public int Period { get; set; }
		public int SMAPeriodK { get; set; }
		public int SMAPeriodD { get; set; }

		public override int RequiredCandleRangeSize => new[] { Period, SMAPeriodD, SMAPeriodK }.Max() + DeviationSize + 1;

		public StochasticSettings()
		{
			Type = IndicatorType.Stochastic;
		}
	}
}
