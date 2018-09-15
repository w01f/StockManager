using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using Ninject;
using StockManager.Dashboard.Controllers;
using StockManager.Dashboard.Models.Chart;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Business.Chart.Models;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Dashboard.Views
{
	public partial class CurrencyPairDashboardControl : XtraUserControl
	{
		private readonly CurrencyPairController _currencyPairController;

		public CurrencyPair Info { get; }

		public CurrencyPairDashboardControl(CurrencyPair info)
		{
			Info = info;
			InitializeComponent();
			Text = Info.Id;
		}

		[Inject]
		public CurrencyPairDashboardControl(CurrencyPair info, CurrencyPairController currencyPairController) : this(info)
		{
			_currencyPairController = currencyPairController;
		}

		public async Task LoadData()
		{
			try
			{
				splashScreenManager.ShowWaitForm();

				var chartSettings = new ChartSettings();
				chartSettings.CurrencyPairId = Info.Id;
				chartSettings.Period = CandlePeriod.Minute5;
				chartSettings.CurrentMoment = new DateTime(2018, 03, 20, 19, 55, 0);

				//chartSettings.CurrentMoment = new DateTime(2018, 03, 19, 2, 0, 0); // 1
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 19, 9, 0, 0); // 2
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 19, 16, 0, 0); // 3
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 19, 23, 0, 0); // 4
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 20, 6, 0, 0); // 5
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 20, 13, 0, 0); // 6
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 20, 20, 0, 0); // 7
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 21, 3, 0, 0); // 8
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 21, 10, 0, 0); // 9
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 21, 17, 0, 0); // 10
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 22, 0, 0, 0); // 11
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 22, 7, 0, 0); // 12
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 22, 14, 0, 0); // 13
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 22, 21, 0, 0); // 14
				//chartSettings.CurrentMoment = new DateTime(2018, 03, 23, 5, 0, 0); // 15
				chartSettings.CurrentMoment = new DateTime(2018, 03, 23, 12, 0, 0); // 16

				//chartSettings.CurrentMoment = new DateTime(2018, 03, 22, 17, 30, 0); // 16


				//TODO Make it optional
				chartSettings.Indicators.AddRange(new IndicatorSettings[]{
					new MACDSettings { CandlePeriod =chartSettings.Period.GetHigherFramePeriod(),  EMAPeriod1 = 12, EMAPeriod2 = 26, SignalPeriod = 9},
					new MACDSettings { CandlePeriod =chartSettings.Period,  EMAPeriod1 = 12, EMAPeriod2 = 26, SignalPeriod = 9},
					new CommonIndicatorSettings {CandlePeriod =chartSettings.Period, Type = IndicatorType.RelativeStrengthIndex, Period = 14},
					new CommonIndicatorSettings {CandlePeriod =chartSettings.Period, Type = IndicatorType.WilliamsR,Period = 5},
					new CommonIndicatorSettings {CandlePeriod =chartSettings.Period, Type = IndicatorType.ParabolicSAR}
				});

				ConfigureIndicatorCharts(chartSettings);

				var chartDataset = await _currencyPairController.GetChartData(chartSettings);

				chartControl.DataSource = BuildOutputDataSet(chartDataset, chartSettings);
				chartControl.RefreshData();

				var diagram = (XYDiagram)chartControl.Diagram;
				diagram.AxisY.WholeRange.SetMinMaxValues(chartDataset.Candles.Min(candle => candle.MinPrice) * 0.999m, chartDataset.Candles.Max(candle => candle.MaxPrice) * 1.001m);
			}
			finally
			{
				splashScreenManager.CloseWaitForm();
			}
		}

		private DataTable BuildOutputDataSet(ChartDataset inputDataset, ChartSettings chartSettings)
		{
			var table = new DataTable("ChartData");

			table.Columns.Add("Moment", typeof(DateTime));
			table.Columns.Add("OpenPrice", typeof(Decimal));
			table.Columns.Add("ClosePrice", typeof(Decimal));
			table.Columns.Add("MaxPrice", typeof(Decimal));
			table.Columns.Add("MinPrice", typeof(Decimal));
			table.Columns.Add("VolumeInBaseCurrency", typeof(Decimal));

			table.Columns.Add("BuyPrice", typeof(Decimal));
			table.Columns.Add("SellPrice", typeof(Decimal));

			table.Columns.AddRange(IndicatorSeriesViewSettings.GetIndicatorSeriesViewSettings(chartSettings)
				.Select(viewSettings => new DataColumn(viewSettings.IndicatorValue, typeof(Decimal))).ToArray());

			foreach (var candle in inputDataset.Candles)
			{
				var rowValues = new List<object>();
				rowValues.Add(candle.Moment);
				rowValues.Add(candle.OpenPrice);
				rowValues.Add(candle.ClosePrice);
				rowValues.Add(candle.MaxPrice);
				rowValues.Add(candle.MinPrice);

				rowValues.Add(candle.VolumeInBaseCurrency);

				var tradingData = inputDataset.TradingData.Single(data => data.Moment == candle.Moment);
				rowValues.Add(tradingData.BuyPrice);
				rowValues.Add(tradingData.SellPrice);

				foreach (var indicatorDataset in inputDataset.IndicatorData)
					switch (indicatorDataset.Settings.Type)
					{
						case IndicatorType.EMA:
							rowValues.Add(indicatorDataset.Values.OfType<SimpleIndicatorValue>()
								.Where(value => value.Moment == candle.Moment)
								.Select(value => value.Value)
								.FirstOrDefault());
							break;
						case IndicatorType.MACD:
							var macdValues = indicatorDataset.Values
								.OfType<MACDValue>()
								.Where(value => value.Moment == candle.Moment)
								.Select(value => new object[] { value.MACD, value.Signal, value.Histogram })
								.FirstOrDefault() ??
								new object[] { DBNull.Value, DBNull.Value, DBNull.Value };
							rowValues.AddRange(macdValues);
							break;
						case IndicatorType.Stochastic:
							var stochasticValues = indicatorDataset.Values
								.OfType<StochasticValue>()
								.Where(value => value.Moment == candle.Moment)
								.Select(value => new object[] { value.K, value.D })
								.FirstOrDefault() ??
								new object[] { DBNull.Value, DBNull.Value };
							rowValues.AddRange(stochasticValues);
							break;
						case IndicatorType.RelativeStrengthIndex:
							rowValues.Add(indicatorDataset.Values.OfType<SimpleIndicatorValue>()
								.Where(value => value.Moment == candle.Moment)
								.Select(value => value.Value)
								.FirstOrDefault());
							break;
						case IndicatorType.AccumulationDistribution:
							rowValues.Add(indicatorDataset.Values.OfType<SimpleIndicatorValue>()
								.Where(value => value.Moment == candle.Moment)
								.Select(value => value.Value)
								.FirstOrDefault());
							break;
						case IndicatorType.WilliamsR:
							rowValues.Add(indicatorDataset.Values.OfType<SimpleIndicatorValue>()
								.Where(value => value.Moment == candle.Moment)
								.Select(value => value.Value)
								.FirstOrDefault());
							break;
						case IndicatorType.ParabolicSAR:
							rowValues.Add(indicatorDataset.Values.OfType<SimpleIndicatorValue>()
								.Where(value => value.Moment == candle.Moment)
								.Select(value => value.Value)
								.FirstOrDefault());
							break;
						default:
							throw new ArgumentOutOfRangeException("Undefined indicator type");
					}
				table.Rows.Add(rowValues.ToArray());
			}

			return table;
		}

		private void ConfigureIndicatorCharts(ChartSettings chartSettings)
		{
			var indicatorSeriesSettingsSet = new List<IndicatorSeriesColorSettings>();
			var indicatorAddtionalPanels = IndicatorPanelSettings.GetAdditionalPanelsSettings(chartSettings.Period);

			//Build Series
			var viewSettingsByIndicatorType = IndicatorSeriesViewSettings.GetIndicatorSeriesViewSettings(chartSettings)
				.GroupBy(viewSettings => new { viewSettings.IndicatorType, viewSettings.CandlePeriod });
			foreach (var seriesViewSettings in viewSettingsByIndicatorType)
			{
				var indicatorSerieses = new List<Series>();
				var seriesViews = new List<SeriesViewColorEachSupportBase>();

				foreach (var viewSettings in seriesViewSettings)
				{
					SeriesViewColorEachSupportBase seriesView;

					switch (viewSettings.ViewType)
					{
						case ViewType.Line:
							seriesView = new LineSeriesView();
							break;
						case ViewType.Bar:
							seriesView = new StackedBarSeriesView();
							break;
						case ViewType.Point:
							seriesView = new PointSeriesView();
							break;
						default:
							throw new ArgumentOutOfRangeException("Undefined chart view type");
					}

					var seriesSettings = new IndicatorSeriesColorSettings
					{
						IndicatorType = seriesViewSettings.Key.IndicatorType,
						CandlePeriod = seriesViewSettings.Key.CandlePeriod,
					};
					var availableColors = IndicatorSeriesColorSettings.AvailableSeriesColors
						.Where(color => indicatorSeriesSettingsSet
							.Where(s => s.IndicatorType == seriesViewSettings.Key.IndicatorType && s.CandlePeriod == seriesViewSettings.Key.CandlePeriod)
							.All(s => s.SeriesColor != color))
						.ToList();
					seriesSettings.SeriesColor = availableColors.Any() ? availableColors.First() : IndicatorSeriesColorSettings.LastDefaultColor;
					indicatorSeriesSettingsSet.Add(seriesSettings);

					seriesView.Color = seriesSettings.SeriesColor;
					seriesViews.Add(seriesView);

					var indicatorSeries = new Series(viewSettings.IndicatorValue, viewSettings.ViewType);
					indicatorSeries.ArgumentScaleType = ScaleType.DateTime;
					indicatorSeries.LabelsVisibility = DevExpress.Utils.DefaultBoolean.False;
					indicatorSeries.ArgumentDataMember = "Moment";
					indicatorSeries.ValueDataMembers.AddRange(viewSettings.IndicatorValue);
					indicatorSeries.View = seriesView;
					indicatorSerieses.Add(indicatorSeries);
				}

				var panelSettings =
					indicatorAddtionalPanels.FirstOrDefault(s =>
						s.AssignedIndicators.Any(tuple => tuple.Item1 == seriesViewSettings.Key.IndicatorType && tuple.Item2 == seriesViewSettings.Key.CandlePeriod));
				if (panelSettings != null)
				{
					if (panelSettings.Panel == null || panelSettings.AxisY == null)
					{
						var pane = new XYDiagramPane();
						((XYDiagram)chartControl.Diagram).Panes.Add(pane);
						panelSettings.Panel = pane;

						var axisY = new SecondaryAxisY();
						((XYDiagram)chartControl.Diagram).SecondaryAxesY.Add(axisY);
						panelSettings.AxisY = axisY;
					}
					foreach (var seriesView in seriesViews)
					{
						seriesView.Pane = panelSettings.Panel;
						seriesView.AxisY = panelSettings.AxisY;
					}
				}
				else
				{
					foreach (var seriesView in seriesViews)
					{
						seriesView.Pane = ((XYDiagram)chartControl.Diagram).DefaultPane;
						seriesView.AxisY = ((XYDiagram)chartControl.Diagram).AxisY;
					}
				}

				chartControl.Series.AddRange(indicatorSerieses.ToArray());
			}
		}
	}
}
