using System.Collections.Generic;
using DevExpress.XtraCharts;
using StockManager.Infrastructure.Business.Common.Models.Chart;

namespace StockManager.Dashboard.Models.Chart
{
	class IndicatorPanelSettings
	{
		public XYDiagramPaneBase Panel { get; set; }
		public SecondaryAxisY AxisY { get; set; }
		public IList<IndicatorType> AssignedIndicators { get; set; }

		public static IList<IndicatorPanelSettings> GetAdditionalPanelsSettings()
		{
			return new[]
			{
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						IndicatorType.MACD,
					}
				},
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						IndicatorType.Stochastic,
					}
				},
			};
		}
	}
}
