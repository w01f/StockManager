using System.Drawing;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Chart.Models;

namespace StockManager.Dashboard.Models.Chart
{
	class IndicatorSeriesColorSettings
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
		public CandlePeriod CandlePeriod { get; set; }
		public Color SeriesColor { get; set; }
	}
}
