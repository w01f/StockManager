namespace StockManager.Infrastructure.Business.Common.Models.Chart
{
	public class BaseIndicatorSettings
	{
		public IndicatorType Type { get; set; }
		public int Period { get; set; }
	}

	public class StochasticSettings : BaseIndicatorSettings
	{
		public int SMACountK { get; set; }
		public int SMACountD { get; set; }

		public StochasticSettings()
		{
			Type = IndicatorType.Stochastic;
		}
	}
}
