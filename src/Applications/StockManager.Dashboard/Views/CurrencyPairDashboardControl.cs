using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using Ninject;
using StockManager.Dashboard.Controllers;
using StockManager.Dashboard.Helpers;
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

				//TODO Make it optional
				chartSettings.Indicators.AddRange(new[]{
					new BaseIndicatorSettings { Type = IndicatorType.EMA, Period = 5 },
					new BaseIndicatorSettings { Type = IndicatorType.EMA, Period = 10 },
					new StochasticSettings { Period = 14, SMACountK = 1, SMACountD = 3}
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

			foreach (var indicatorDataset in inputDataset.IndicatorData)
				table.Columns.AddRange(indicatorDataset.Settings.GetIndicatorTitles().Select(title => new DataColumn(title, typeof(Decimal))).ToArray());

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

		private void ConfigureIndicatorChars(IList<BaseIndicatorSettings> indicators)
		{
			var indicatorSeriesSettingsSet = new List<IndicatorSeriesSettings>();
			var indicatorAddtionalPanels = IndicatorPanelSettings.GetAdditionalPanelsSettings();

			//Build Series
			foreach (var indicator in indicators)
			{
				var indicatorSerieses = new List<Series>();
				var seriesViews = new List<LineSeriesView>();

				var indicatorParts = indicator.GetIndicatorTitles().ToList();
				if (!indicatorParts.Any())
					throw new ArgumentOutOfRangeException("No indicator parts for selected indicator");

				foreach (var indicatorTitle in indicatorParts)
				{
					var seriesView = new LineSeriesView();
					var seriesSettings = new IndicatorSeriesSettings
					{
						IndicatorType = indicator.Type
					};
					var availableColors = IndicatorSeriesSettings.AvailableSeriesColors
						.Where(color => indicatorSeriesSettingsSet.Where(s => s.IndicatorType == indicator.Type).All(s => s.SeriesColor != color))
						.ToList();
					seriesSettings.SeriesColor = availableColors.Any() ? availableColors.First() : IndicatorSeriesSettings.LastDefaultColor;
					indicatorSeriesSettingsSet.Add(seriesSettings);

					seriesView.Color = seriesSettings.SeriesColor;
					seriesViews.Add(seriesView);

					var indicatorSeries = new Series(indicatorTitle, ViewType.Line);
					indicatorSeries.ArgumentScaleType = ScaleType.DateTime;
					indicatorSeries.LabelsVisibility = DevExpress.Utils.DefaultBoolean.False;
					indicatorSeries.ArgumentDataMember = "Moment";
					indicatorSeries.ValueDataMembers.AddRange(indicatorTitle);
					indicatorSeries.View = seriesView;
					indicatorSerieses.Add(indicatorSeries);
				}

				var panelSettings = indicatorAddtionalPanels.FirstOrDefault(s => s.AssignedIndicators.Contains(indicator.Type));
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
