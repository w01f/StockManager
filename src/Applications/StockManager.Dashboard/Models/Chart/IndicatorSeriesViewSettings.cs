using System;
using System.Collections.Generic;
using DevExpress.XtraCharts;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Chart.Models;

namespace StockManager.Dashboard.Models.Chart
{
	class IndicatorSeriesViewSettings
	{
		public IndicatorType IndicatorType { get; set; }
		public CandlePeriod CandlePeriod { get; set; }
		public string IndicatorValue { get; set; }
		public ViewType ViewType { get; set; }

		public static IList<IndicatorSeriesViewSettings> GetIndicatorSeriesViewSettings(ChartSettings chartSettings)
		{
			var viewSettings = new List<IndicatorSeriesViewSettings>();

			foreach (var indicatorSetting in chartSettings.Indicators)
			{
				switch (indicatorSetting.Type)
				{
					case IndicatorType.EMA:
						viewSettings.Add(new IndicatorSeriesViewSettings
						{
							IndicatorType = IndicatorType.EMA,
							CandlePeriod = indicatorSetting.CandlePeriod,
							IndicatorValue = String.Format("{0}{1}", indicatorSetting.Type.ToString(), ((CommonIndicatorSettings)indicatorSetting).Period),
							ViewType = ViewType.Line
						});
						break;
					case IndicatorType.MACD:

						ViewType macdViewType;
						if (indicatorSetting.CandlePeriod == chartSettings.Period)
							macdViewType = ViewType.Line;
						else
							macdViewType = ViewType.Point;

						viewSettings.AddRange(new[]
						{
							new IndicatorSeriesViewSettings
							{
								IndicatorType = IndicatorType.MACD,
								CandlePeriod = indicatorSetting.CandlePeriod,
								IndicatorValue = String.Format("{0}_{1}_{2}_{3}_{4}MACD",
									indicatorSetting.Type.ToString(),
									indicatorSetting.CandlePeriod.ToString(),
									((MACDSettings)indicatorSetting).EMAPeriod1,
									((MACDSettings)indicatorSetting).EMAPeriod2,
									((MACDSettings)indicatorSetting).SignalPeriod),
								ViewType = macdViewType
							},
							new IndicatorSeriesViewSettings
							{
								IndicatorType = IndicatorType.MACD,
								CandlePeriod = indicatorSetting.CandlePeriod,
								IndicatorValue = String.Format("{0}_{1}_{2}_{3}_{4}Signal",
									indicatorSetting.Type.ToString(),
									indicatorSetting.CandlePeriod.ToString(),
									((MACDSettings)indicatorSetting).EMAPeriod1,
									((MACDSettings)indicatorSetting).EMAPeriod2,
									((MACDSettings)indicatorSetting).SignalPeriod),
								ViewType = macdViewType
							},
							new IndicatorSeriesViewSettings
							{
								IndicatorType = IndicatorType.MACD,
								CandlePeriod = indicatorSetting.CandlePeriod,
								IndicatorValue = String.Format("{0}_{1}_{2}_{3}_{4}Histogram",
									indicatorSetting.Type.ToString(),
									indicatorSetting.CandlePeriod.ToString(),
									((MACDSettings)indicatorSetting).EMAPeriod1,
									((MACDSettings)indicatorSetting).EMAPeriod2,
									((MACDSettings)indicatorSetting).SignalPeriod),
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
								CandlePeriod = indicatorSetting.CandlePeriod,
								IndicatorValue = String.Format("{0}_{1}_{2}_{3}_{4}K",
									indicatorSetting.Type.ToString(),
									indicatorSetting.CandlePeriod.ToString(),
									((StochasticSettings)indicatorSetting).Period,
									((StochasticSettings)indicatorSetting).SMAPeriodK,
									((StochasticSettings)indicatorSetting).SMAPeriodD),
								ViewType = ViewType.Line
							},
							new IndicatorSeriesViewSettings
							{
								IndicatorType = IndicatorType.Stochastic,
								CandlePeriod = indicatorSetting.CandlePeriod,
								IndicatorValue = String.Format("{0}_{1}_{2}_{3}_{4}D",
									indicatorSetting.Type.ToString(),
									indicatorSetting.CandlePeriod.ToString(),
									((StochasticSettings)indicatorSetting).Period,
									((StochasticSettings)indicatorSetting).SMAPeriodK,
									((StochasticSettings)indicatorSetting).SMAPeriodD),
								ViewType = ViewType.Line
							},
						});
						break;
					case IndicatorType.RelativeStrengthIndex:
						viewSettings.Add(new IndicatorSeriesViewSettings
						{
							IndicatorType = IndicatorType.RelativeStrengthIndex,
							CandlePeriod = indicatorSetting.CandlePeriod,
							IndicatorValue = String.Format("{0}_{1}_{2}",
								indicatorSetting.Type.ToString(),
								indicatorSetting.CandlePeriod.ToString(),
								((CommonIndicatorSettings)indicatorSetting).Period),
							ViewType = ViewType.Line
						});
						break;
					case IndicatorType.AccumulationDistribution:
						viewSettings.Add(new IndicatorSeriesViewSettings
						{
							IndicatorType = IndicatorType.AccumulationDistribution,
							CandlePeriod = indicatorSetting.CandlePeriod,
							IndicatorValue = String.Format("{0}_{1}_{2}",
								indicatorSetting.Type.ToString(),
								indicatorSetting.CandlePeriod.ToString(),
								((CommonIndicatorSettings)indicatorSetting).Period),
							ViewType = ViewType.Line
						});
						break;
					case IndicatorType.WilliamsR:
						viewSettings.Add(new IndicatorSeriesViewSettings
						{
							IndicatorType = IndicatorType.WilliamsR,
							CandlePeriod = indicatorSetting.CandlePeriod,
							IndicatorValue = String.Format("{0}_{1}_{2}",
								indicatorSetting.Type.ToString(),
								indicatorSetting.CandlePeriod.ToString(),
								((CommonIndicatorSettings)indicatorSetting).Period),
							ViewType = ViewType.Line
						});
						break;
					case IndicatorType.ParabolicSAR:
						viewSettings.Add(new IndicatorSeriesViewSettings
						{
							IndicatorType = IndicatorType.ParabolicSAR,
							CandlePeriod = indicatorSetting.CandlePeriod,
							IndicatorValue = String.Format("{0}{1}", indicatorSetting.Type.ToString(), ((CommonIndicatorSettings)indicatorSetting).Period),
							ViewType = ViewType.Point
						});
						break;
				}
			}

			return viewSettings;
		}
	}
}
