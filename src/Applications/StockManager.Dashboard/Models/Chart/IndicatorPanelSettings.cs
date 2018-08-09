using System;
using System.Collections.Generic;
using DevExpress.XtraCharts;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Infrastructure.Business.Chart.Models;

namespace StockManager.Dashboard.Models.Chart
{
	class IndicatorPanelSettings
	{
		public XYDiagramPaneBase Panel { get; set; }
		public SecondaryAxisY AxisY { get; set; }
		public IList<Tuple<IndicatorType, CandlePeriod>> AssignedIndicators { get; set; }

		public static IList<IndicatorPanelSettings> GetAdditionalPanelsSettings()
		{
			return new[]
			{
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						new Tuple<IndicatorType, CandlePeriod>(IndicatorType.MACD, CandlePeriod.Minute30),
					}
				},
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						new Tuple<IndicatorType, CandlePeriod>(IndicatorType.MACD, CandlePeriod.Minute5),
					}
				},
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						new Tuple<IndicatorType, CandlePeriod>(IndicatorType.Stochastic, CandlePeriod.Minute5),
					}
				},
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						new Tuple<IndicatorType, CandlePeriod>(IndicatorType.RelativeStrengthIndex, CandlePeriod.Minute5),
					}
				},
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						new Tuple<IndicatorType, CandlePeriod>(IndicatorType.AccumulationDistribution, CandlePeriod.Minute5),
					}
				},
			};
		}
	}
}
