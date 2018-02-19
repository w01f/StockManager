using System;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using Ninject;
using StockManager.Dashboard.Controllers;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Dashboard.Views
{
	public partial class CurrencyPairDashboardControl : XtraUserControl
	{
		private readonly MarketController _marketController;

		public CurrencyPair Info { get; }

		public CurrencyPairDashboardControl(CurrencyPair info)
		{
			Info = info;
			InitializeComponent();
			Text = Info.Id;
		}

		[Inject]
		public CurrencyPairDashboardControl(CurrencyPair info, MarketController marketController) : this(info)
		{
			_marketController = marketController;
		}

		public async Task LoadData()
		{
			try
			{
				splashScreenManager.ShowWaitForm();
				var candles = (await _marketController.GetCandles(Info.Id, CandlePeriod.Minute15)).ToList();

				chartControl.DataSource = candles;
				chartControl.RefreshData();

				var diagram = (XYDiagram)chartControl.Diagram;
				var totalCandlesCount = candles.Count;
				var visibleCandles = candles.Skip(totalCandlesCount - (Int32)(totalCandlesCount * 0.1)).ToList();
				diagram.AxisX.VisualRange.SetMinMaxValues(visibleCandles.Min(candle => candle.Moment), visibleCandles.Max(candle => candle.Moment));
				diagram.AxisY.VisualRange.SetMinMaxValues(visibleCandles.Min(candle => candle.MinPrice), visibleCandles.Max(candle => candle.MaxPrice));
			}
			finally
			{
				splashScreenManager.CloseWaitForm();
			}
		}
	}
}
