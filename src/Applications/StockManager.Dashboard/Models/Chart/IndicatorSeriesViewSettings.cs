using System;
using System.Collections.Generic;
using DevExpress.XtraCharts;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Infrastructure.Business.Chart.Models;

namespace StockManager.Dashboard.Models.Chart
{
	class IndicatorSeriesViewSettings
	{
		public IndicatorType IndicatorType { get; set; }
		public CandlePeriod CandlePeriod { get; set; }
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
							CandlePeriod = indicatorSetting.CandlePeriod,
							IndicatorValue = String.Format("{0}{1}", indicatorSetting.Type.ToString(), ((CommonIndicatorSettings)indicatorSetting).Period),
							ViewType = ViewType.Line
						});
						break;
					case IndicatorType.MACD:

						ViewType macdViewType;
						switch (indicatorSetting.CandlePeriod)
						{
							case CandlePeriod.Minute5:
								macdViewType = ViewType.Line;
								break;
							default:
								macdViewType = ViewType.Point;
								break;
						}

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
				}
			}

			return viewSettings;
		}
	}
}
