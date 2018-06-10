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
using StockManager.Domain.Core.Common.Enums;
using StockManager.Infrastructure.Business.Common.Models.Chart;
using StockManager.Infrastructure.Common.Models.Analysis;
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
				chartSettings.CurrentMoment = new DateTime(2018, 03, 23, 12, 0, 0);

				//TODO Make it optional
				chartSettings.Indicators.AddRange(new IndicatorSettings[]{
					new CommonIndicatorSettings { Type = IndicatorType.EMA, Period = 5 },
					new CommonIndicatorSettings { Type = IndicatorType.EMA, Period = 10 },
					new MACDSettings { EMAPeriod1 = 12, EMAPeriod2 = 26, SignalPeriod = 9},
					new StochasticSettings { Period = 14, SMAPeriodK = 1, SMAPeriodD = 3}
				});

				var chartDataset = await _currencyPairController.GetChartData(chartSettings);

				ConfigureIndicatorChars(chartSettings.Indicators);

				chartControl.DataSource = BuildOutputDataSet(chartDataset);
				chartControl.RefreshData();

				var diagram = (XYDiagram)chartControl.Diagram;
				var totalCandlesCount = chartDataset.Candles.Count;
				var visibleCandles = chartDataset.Candles.Skip(totalCandlesCount - (Int32)(totalCandlesCount * 0.1)).ToList();
				diagram.AxisX.VisualRange.SetMinMaxValues(visibleCandles.Min(candle => candle.Moment), visibleCandles.Max(candle => candle.Moment));
				diagram.AxisY.VisualRange.SetMinMaxValues(visibleCandles.Min(candle => candle.MinPrice), visibleCandles.Max(candle => candle.MaxPrice));
			}
			finally
			{
				splashScreenManager.CloseWaitForm();
			}
		}

		private DataTable BuildOutputDataSet(ChartDataset inputDataset)
		{
			var table = new DataTable("ChartData");

			table.Columns.Add("Moment", typeof(DateTime));
			table.Columns.Add("OpenPrice", typeof(Decimal));
			table.Columns.Add("ClosePrice", typeof(Decimal));
			table.Columns.Add("MaxPrice", typeof(Decimal));
			table.Columns.Add("MinPrice", typeof(Decimal));

			table.Columns.AddRange(IndicatorSeriesViewSettings.GetIndicatorSeriesViewSettings(inputDataset.IndicatorData.Select(data => data.Settings).ToList()).Select(viewSettings => new DataColumn(viewSettings.IndicatorValue, typeof(Decimal))).ToArray());

			foreach (var candle in inputDataset.Candles)
			{
				var rowValues = new List<object>();
				rowValues.Add(candle.Moment);
				rowValues.Add(candle.OpenPrice);
				rowValues.Add(candle.ClosePrice);
				rowValues.Add(candle.MaxPrice);
				rowValues.Add(candle.MinPrice);

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
							rowValues.AddRange(indicatorDataset.Values.OfType<MACDValue>()
								.Where(value => value.Moment == candle.Moment)
								.SelectMany(value => new object[] { value.MACD, value.Signal, value.Histogram }));
							break;
						case IndicatorType.Stochastic:
							rowValues.AddRange(indicatorDataset.Values.OfType<StochasticValue>()
								.Where(value => value.Moment == candle.Moment)
								.SelectMany(value => new object[] { value.K, value.D }));
							break;
						default:
							throw new ArgumentOutOfRangeException("Undefined indicator type");
					}
				table.Rows.Add(rowValues.ToArray());
			}

			return table;
		}

		private void ConfigureIndicatorChars(IList<IndicatorSettings> indicators)
		{
			var indicatorSeriesSettingsSet = new List<IndicatorSeriesColorSettings>();
			var indicatorAddtionalPanels = IndicatorPanelSettings.GetAdditionalPanelsSettings();

			//Build Series
			var viewSettingsByIndicatorType = IndicatorSeriesViewSettings.GetIndicatorSeriesViewSettings(indicators).GroupBy(viewSettings => viewSettings.IndicatorType);
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
						default:
							throw new ArgumentOutOfRangeException("Undefined chart view type");
					}

					var seriesSettings = new IndicatorSeriesColorSettings
					{
						IndicatorType = seriesViewSettings.Key
					};
					var availableColors = IndicatorSeriesColorSettings.AvailableSeriesColors
						.Where(color => indicatorSeriesSettingsSet.Where(s => s.IndicatorType == seriesViewSettings.Key).All(s => s.SeriesColor != color))
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

				var panelSettings = indicatorAddtionalPanels.FirstOrDefault(s => s.AssignedIndicators.Contains(seriesViewSettings.Key));
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
