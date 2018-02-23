using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using Ninject;
using StockManager.Dashboard.Controllers;
using StockManager.Dashboard.Helpers;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Infrastructure.Business.Common.Models.Chart;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Dashboard.Views
{
	public partial class CurrencyPairDashboardControl : XtraUserControl
	{
		private readonly CurrencyPairController _currencyPairController;
		private readonly List<IndicatorSettings> _indicatorSettings = new List<IndicatorSettings>();

		public CurrencyPair Info { get; }

		public CurrencyPairDashboardControl(CurrencyPair info)
		{
			Info = info;
			InitializeComponent();
			Text = Info.Id;

			InitIndicators();
		}

		[Inject]
		public CurrencyPairDashboardControl(CurrencyPair info, CurrencyPairController currencyPairController) : this(info)
		{
			_currencyPairController = currencyPairController;
		}

		//TODO Make it optional
		private void InitIndicators()
		{
			{
				var indicator = new IndicatorSettings { Type = IndicatorType.EMA, Period = 5 };
				_indicatorSettings.Add(indicator);

				var indicatorSeries = new Series(indicator.GetIndicatorTitle(), ViewType.Spline);
				indicatorSeries.ArgumentScaleType = ScaleType.DateTime;
				indicatorSeries.LabelsVisibility = DevExpress.Utils.DefaultBoolean.False;
				indicatorSeries.ArgumentDataMember = "Moment";
				indicatorSeries.ValueDataMembers.AddRange(indicator.GetIndicatorTitle());

				var seriesView = new SplineSeriesView();
				seriesView.Color = Color.Orange;
				indicatorSeries.View = seriesView;

				chartControl.Series.Add(indicatorSeries);
			}

			{
				var indicator = new IndicatorSettings { Type = IndicatorType.EMA, Period = 10 };
				_indicatorSettings.Add(indicator);

				var indicatorSeries = new Series(indicator.GetIndicatorTitle(), ViewType.Spline);
				indicatorSeries.ArgumentScaleType = ScaleType.DateTime;
				indicatorSeries.LabelsVisibility = DevExpress.Utils.DefaultBoolean.False;
				indicatorSeries.ArgumentDataMember = "Moment";
				indicatorSeries.ValueDataMembers.AddRange(indicator.GetIndicatorTitle());

				var seriesView = new SplineSeriesView();
				seriesView.Color = Color.Magenta;
				indicatorSeries.View = seriesView;

				chartControl.Series.Add(indicatorSeries);
			}
		}

		public async Task LoadData()
		{
			try
			{
				splashScreenManager.ShowWaitForm();

				var chartSettings = new ChartSettings();
				chartSettings.CurrencyPairId = Info.Id;
				chartSettings.Period = CandlePeriod.Minute15;
				chartSettings.Indicators.AddRange(_indicatorSettings);

				var chartDataset = (await _currencyPairController.GetChartData(chartSettings));

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
				table.Columns.Add(indicatorDataset.Settings.GetIndicatorTitle(), typeof(Decimal));

			foreach (var candle in inputDataset.Candles)
			{
				var rowValues = new List<object>();
				rowValues.Add(candle.Moment);
				rowValues.Add(candle.OpenPrice);
				rowValues.Add(candle.ClosePrice);
				rowValues.Add(candle.MaxPrice);
				rowValues.Add(candle.MinPrice);

				foreach (var indicatorDataset in inputDataset.IndicatorData)
					rowValues.Add(indicatorDataset.Values
						.Where(value => value.Moment == candle.Moment)
						.Select(value => value.Value)
						.FirstOrDefault());

				table.Rows.Add(rowValues.ToArray());
			}

			return table;
		}
	}
}
