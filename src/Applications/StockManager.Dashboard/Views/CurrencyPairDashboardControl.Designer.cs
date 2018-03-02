namespace StockManager.Dashboard.Views
{
	partial class CurrencyPairDashboardControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			DevExpress.XtraCharts.XYDiagram xyDiagram1 = new DevExpress.XtraCharts.XYDiagram();
			DevExpress.XtraCharts.Series series1 = new DevExpress.XtraCharts.Series();
			DevExpress.XtraCharts.CandleStickSeriesView candleStickSeriesView1 = new DevExpress.XtraCharts.CandleStickSeriesView();
			this.chartControl = new DevExpress.XtraCharts.ChartControl();
			this.splashScreenManager = new DevExpress.XtraSplashScreen.SplashScreenManager(this, typeof(global::StockManager.Dashboard.Views.FormProgress), true, true, typeof(System.Windows.Forms.UserControl));
			((System.ComponentModel.ISupportInitialize)(this.chartControl)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(xyDiagram1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(series1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(candleStickSeriesView1)).BeginInit();
			this.SuspendLayout();
			// 
			// chartControl
			// 
			this.chartControl.CrosshairOptions.ShowOnlyInFocusedPane = false;
			this.chartControl.DataBindings = null;
			xyDiagram1.AxisX.AutoScaleBreaks.Enabled = true;
			xyDiagram1.AxisX.DateTimeScaleOptions.AggregateFunction = DevExpress.XtraCharts.AggregateFunction.None;
			xyDiagram1.AxisX.DateTimeScaleOptions.MeasureUnit = DevExpress.XtraCharts.DateTimeMeasureUnit.Minute;
			xyDiagram1.AxisX.Tickmarks.MinorVisible = false;
			xyDiagram1.AxisX.VisibleInPanesSerializable = "-1";
			xyDiagram1.AxisY.Alignment = DevExpress.XtraCharts.AxisAlignment.Far;
			xyDiagram1.AxisY.VisibleInPanesSerializable = "-1";
			xyDiagram1.EnableAxisXScrolling = true;
			xyDiagram1.EnableAxisXZooming = true;
			xyDiagram1.EnableAxisYScrolling = true;
			xyDiagram1.EnableAxisYZooming = true;
			this.chartControl.Diagram = xyDiagram1;
			this.chartControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.chartControl.Legend.Name = "Default Legend";
			this.chartControl.Legend.Visibility = DevExpress.Utils.DefaultBoolean.False;
			this.chartControl.Location = new System.Drawing.Point(0, 0);
			this.chartControl.Name = "chartControl";
			series1.ArgumentDataMember = "Moment";
			series1.ArgumentScaleType = DevExpress.XtraCharts.ScaleType.DateTime;
			series1.LabelsVisibility = DevExpress.Utils.DefaultBoolean.False;
			series1.LegendName = "Default Legend";
			series1.Name = "Candles";
			series1.ValueDataMembersSerializable = "MinPrice;MaxPrice;OpenPrice;ClosePrice";
			candleStickSeriesView1.LevelLineLength = 0.8D;
			candleStickSeriesView1.LineThickness = 1;
			series1.View = candleStickSeriesView1;
			this.chartControl.SeriesSerializable = new DevExpress.XtraCharts.Series[] {
        series1};
			this.chartControl.Size = new System.Drawing.Size(492, 481);
			this.chartControl.TabIndex = 0;
			// 
			// splashScreenManager
			// 
			this.splashScreenManager.ClosingDelay = 500;
			// 
			// CurrencyPairDashboardControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.chartControl);
			this.Name = "CurrencyPairDashboardControl";
			this.Size = new System.Drawing.Size(492, 481);
			((System.ComponentModel.ISupportInitialize)(xyDiagram1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(candleStickSeriesView1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(series1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.chartControl)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private DevExpress.XtraCharts.ChartControl chartControl;
		private DevExpress.XtraSplashScreen.SplashScreenManager splashScreenManager;
	}
}
