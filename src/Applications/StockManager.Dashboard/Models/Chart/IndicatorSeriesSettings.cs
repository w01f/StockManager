using System.Drawing;
using StockManager.Infrastructure.Business.Common.Models.Chart;

namespace StockManager.Dashboard.Models.Chart
{
	class IndicatorSeriesSettings
	{
		public static Color[] AvailableSeriesColors = {
			Color.Orange,
			Color.Magenta,
			Color.Green,
			Color.Blue,
			Color.Yellow,
			Color.DeepSkyBlue,
		};
		public static Color LastDefaultColor = Color.Brown;

		public IndicatorType IndicatorType { get; set; }
		public Color SeriesColor { get; set; }
	}
}
