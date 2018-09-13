using System;
using System.Collections.Generic;
using DevExpress.XtraCharts;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Chart.Models;
using StockManager.Infrastructure.Business.Trading.Helpers;

namespace StockManager.Dashboard.Models.Chart
{
	class IndicatorPanelSettings
	{
		public XYDiagramPaneBase Panel { get; set; }
		public SecondaryAxisY AxisY { get; set; }
		public IList<Tuple<IndicatorType, CandlePeriod>> AssignedIndicators { get; set; }

		public static IList<IndicatorPanelSettings> GetAdditionalPanelsSettings(CandlePeriod period)
		{
			return new[]
			{
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						new Tuple<IndicatorType, CandlePeriod>(IndicatorType.MACD, period.GetHigherFramePeriod()),
					}
				},
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						new Tuple<IndicatorType, CandlePeriod>(IndicatorType.MACD, period),
					}
				},
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						new Tuple<IndicatorType, CandlePeriod>(IndicatorType.Stochastic, period),
					}
				},
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						new Tuple<IndicatorType, CandlePeriod>(IndicatorType.RelativeStrengthIndex, period),
					}
				},
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						new Tuple<IndicatorType, CandlePeriod>(IndicatorType.AccumulationDistribution, period),
					}
				},
				new IndicatorPanelSettings
				{
					AssignedIndicators = new[]
					{
						new Tuple<IndicatorType, CandlePeriod>(IndicatorType.WilliamsR, period),
					}
				},
			};
		}
	}
}
