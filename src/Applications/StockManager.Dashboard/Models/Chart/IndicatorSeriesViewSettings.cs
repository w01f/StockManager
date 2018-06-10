using System;
using System.Collections.Generic;
using DevExpress.XtraCharts;
using StockManager.Infrastructure.Business.Common.Models.Chart;

namespace StockManager.Dashboard.Models.Chart
{
	class IndicatorSeriesViewSettings
	{
		public IndicatorType IndicatorType { get; set; }
		public string IndicatorValue { get; set; }
		public ViewType ViewType { get; set; }


		public static IList<IndicatorSeriesViewSettings> GetIndicatorSeriesViewSettings(IList<IndicatorSettings> indicatorSettings)
		{
			var viewSettings = new List<IndicatorSeriesViewSettings>();

			foreach (var indicatorSetting in indicatorSettings)
			{
				switch (indicatorSetting.Type)
				{
					case IndicatorType.EMA:
						viewSettings.Add(new IndicatorSeriesViewSettings
						{
							IndicatorType = IndicatorType.EMA,
							IndicatorValue = String.Format("{0}{1}", indicatorSetting.Type.ToString(), ((CommonIndicatorSettings)indicatorSetting).Period),
							ViewType = ViewType.Line
						});
						break;
					case IndicatorType.MACD:
						viewSettings.AddRange(new[]
						{
							new IndicatorSeriesViewSettings
							{
								IndicatorType = IndicatorType.MACD,
								IndicatorValue = String.Format("{0}{1}_{2}_{3}MACD", indicatorSetting.Type.ToString(), ((MACDSettings)indicatorSetting).EMAPeriod1, ((MACDSettings)indicatorSetting).EMAPeriod2, ((MACDSettings)indicatorSetting).SignalPeriod),
								ViewType = ViewType.Line
							},
							new IndicatorSeriesViewSettings
							{
								IndicatorType = IndicatorType.MACD,
								IndicatorValue = String.Format("{0}{1}_{2}_{3}Signal", indicatorSetting.Type.ToString(), ((MACDSettings)indicatorSetting).EMAPeriod1, ((MACDSettings)indicatorSetting).EMAPeriod2, ((MACDSettings)indicatorSetting).SignalPeriod),
								ViewType = ViewType.Line
							},
							new IndicatorSeriesViewSettings
							{
								IndicatorType = IndicatorType.MACD,
								IndicatorValue = String.Format("{0}{1}_{2}_{3}Histogram", indicatorSetting.Type.ToString(), ((MACDSettings)indicatorSetting).EMAPeriod1, ((MACDSettings)indicatorSetting).EMAPeriod2, ((MACDSettings)indicatorSetting).SignalPeriod),
								ViewType = ViewType.Bar
							},
						});
						break;
					case IndicatorType.Stochastic:
						viewSettings.AddRange(new[]
						{
							new IndicatorSeriesViewSettings
							{
								IndicatorType = IndicatorType.Stochastic,
								IndicatorValue = String.Format("{0}{1}_{2}_{3}K", indicatorSetting.Type.ToString(), ((StochasticSettings)indicatorSetting).Period,((StochasticSettings)indicatorSetting).SMAPeriodK,((StochasticSettings)indicatorSetting).SMAPeriodD),
								ViewType = ViewType.Line
							},
							new IndicatorSeriesViewSettings
							{
								IndicatorType = IndicatorType.Stochastic,
								IndicatorValue = String.Format("{0}{1}_{2}_{3}D", indicatorSetting.Type.ToString(), ((StochasticSettings)indicatorSetting).Period,((StochasticSettings)indicatorSetting).SMAPeriodK,((StochasticSettings)indicatorSetting).SMAPeriodD),
								ViewType = ViewType.Line
							},
						});
						break;
				}
			}

			return viewSettings;
		}
	}
}
