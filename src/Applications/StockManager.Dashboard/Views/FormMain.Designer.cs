namespace StockManager.Dashboard.Views
{
	partial class FormMain
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
			this.components = new System.ComponentModel.Container();
			this.ribbonControl = new DevExpress.XtraBars.Ribbon.RibbonControl();
			this.ribbonPageMarket = new DevExpress.XtraBars.Ribbon.RibbonPage();
			this.ribbonStatusBar = new DevExpress.XtraBars.Ribbon.RibbonStatusBar();
			this.dockManager = new DevExpress.XtraBars.Docking.DockManager(this.components);
			this.dockPanel = new DevExpress.XtraBars.Docking.DockPanel();
			this.dockPanel_Container = new DevExpress.XtraBars.Docking.ControlContainer();
			this.accordionControl = new DevExpress.XtraBars.Navigation.AccordionControl();
			this.tabbedView = new DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView(this.components);
			this.documentManager = new DevExpress.XtraBars.Docking2010.DocumentManager(this.components);
			this.defaultLookAndFeel = new DevExpress.LookAndFeel.DefaultLookAndFeel(this.components);
			this.splashScreenManager = new DevExpress.XtraSplashScreen.SplashScreenManager(this, typeof(global::StockManager.Dashboard.Views.FormProgress), true, true);
			((System.ComponentModel.ISupportInitialize)(this.ribbonControl)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.dockManager)).BeginInit();
			this.dockPanel.SuspendLayout();
			this.dockPanel_Container.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.accordionControl)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.tabbedView)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.documentManager)).BeginInit();
			this.SuspendLayout();
			// 
			// ribbonControl
			// 
			this.ribbonControl.ExpandCollapseItem.Id = 0;
			this.ribbonControl.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.ribbonControl.ExpandCollapseItem});
			this.ribbonControl.Location = new System.Drawing.Point(0, 0);
			this.ribbonControl.MaxItemId = 1;
			this.ribbonControl.MdiMergeStyle = DevExpress.XtraBars.Ribbon.RibbonMdiMergeStyle.Always;
			this.ribbonControl.Name = "ribbonControl";
			this.ribbonControl.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] {
            this.ribbonPageMarket});
			this.ribbonControl.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonControlStyle.Office2013;
			this.ribbonControl.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
			this.ribbonControl.ShowDisplayOptionsMenuButton = DevExpress.Utils.DefaultBoolean.False;
			this.ribbonControl.Size = new System.Drawing.Size(1000, 146);
			this.ribbonControl.StatusBar = this.ribbonStatusBar;
			this.ribbonControl.ToolbarLocation = DevExpress.XtraBars.Ribbon.RibbonQuickAccessToolbarLocation.Hidden;
			// 
			// ribbonPageMarket
			// 
			this.ribbonPageMarket.Name = "ribbonPageMarket";
			this.ribbonPageMarket.Text = "Market";
			// 
			// ribbonStatusBar
			// 
			this.ribbonStatusBar.Location = new System.Drawing.Point(0, 685);
			this.ribbonStatusBar.Name = "ribbonStatusBar";
			this.ribbonStatusBar.Ribbon = this.ribbonControl;
			this.ribbonStatusBar.Size = new System.Drawing.Size(1000, 21);
			// 
			// dockManager
			// 
			this.dockManager.DockingOptions.HideImmediatelyOnAutoHide = true;
			this.dockManager.Form = this;
			this.dockManager.RootPanels.AddRange(new DevExpress.XtraBars.Docking.DockPanel[] {
            this.dockPanel});
			this.dockManager.TopZIndexControls.AddRange(new string[] {
            "DevExpress.XtraBars.BarDockControl",
            "DevExpress.XtraBars.StandaloneBarDockControl",
            "System.Windows.Forms.StatusBar",
            "System.Windows.Forms.MenuStrip",
            "System.Windows.Forms.StatusStrip",
            "DevExpress.XtraBars.Ribbon.RibbonStatusBar",
            "DevExpress.XtraBars.Ribbon.RibbonControl",
            "DevExpress.XtraBars.Navigation.OfficeNavigationBar",
            "DevExpress.XtraBars.Navigation.TileNavPane"});
			// 
			// dockPanel
			// 
			this.dockPanel.Controls.Add(this.dockPanel_Container);
			this.dockPanel.Dock = DevExpress.XtraBars.Docking.DockingStyle.Left;
			this.dockPanel.ID = new System.Guid("a045df26-1503-4d9a-99c1-a531310af22b");
			this.dockPanel.Location = new System.Drawing.Point(0, 146);
			this.dockPanel.Name = "dockPanel";
			this.dockPanel.Options.AllowDockAsTabbedDocument = false;
			this.dockPanel.Options.AllowDockBottom = false;
			this.dockPanel.Options.AllowDockFill = false;
			this.dockPanel.Options.AllowDockRight = false;
			this.dockPanel.Options.AllowDockTop = false;
			this.dockPanel.Options.AllowFloating = false;
			this.dockPanel.Options.FloatOnDblClick = false;
			this.dockPanel.Options.ShowCloseButton = false;
			this.dockPanel.Options.ShowMaximizeButton = false;
			this.dockPanel.OriginalSize = new System.Drawing.Size(208, 200);
			this.dockPanel.Size = new System.Drawing.Size(208, 539);
			this.dockPanel.Text = "Pairs";
			// 
			// dockPanel_Container
			// 
			this.dockPanel_Container.Controls.Add(this.accordionControl);
			this.dockPanel_Container.Location = new System.Drawing.Point(4, 38);
			this.dockPanel_Container.Name = "dockPanel_Container";
			this.dockPanel_Container.Size = new System.Drawing.Size(199, 497);
			this.dockPanel_Container.TabIndex = 0;
			// 
			// accordionControl
			// 
			this.accordionControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.accordionControl.Location = new System.Drawing.Point(0, 0);
			this.accordionControl.Name = "accordionControl";
			this.accordionControl.ScrollBarMode = DevExpress.XtraBars.Navigation.ScrollBarMode.Auto;
			this.accordionControl.Size = new System.Drawing.Size(199, 497);
			this.accordionControl.TabIndex = 0;
			this.accordionControl.Text = "accordionControl";
			// 
			// tabbedView
			// 
			this.tabbedView.RootContainer.Element = null;
			// 
			// documentManager
			// 
			this.documentManager.ContainerControl = this;
			this.documentManager.RibbonAndBarsMergeStyle = DevExpress.XtraBars.Docking2010.Views.RibbonAndBarsMergeStyle.Always;
			this.documentManager.View = this.tabbedView;
			this.documentManager.ViewCollection.AddRange(new DevExpress.XtraBars.Docking2010.Views.BaseView[] {
            this.tabbedView});
			// 
			// defaultLookAndFeel
			// 
			this.defaultLookAndFeel.LookAndFeel.SkinName = "Office 2016 Colorful";
			// 
			// splashScreenManager
			// 
			this.splashScreenManager.ClosingDelay = 500;
			// 
			// FormMain
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(1000, 706);
			this.Controls.Add(this.dockPanel);
			this.Controls.Add(this.ribbonStatusBar);
			this.Controls.Add(this.ribbonControl);
			this.Name = "FormMain";
			this.Ribbon = this.ribbonControl;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.StatusBar = this.ribbonStatusBar;
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			((System.ComponentModel.ISupportInitialize)(this.ribbonControl)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.dockManager)).EndInit();
			this.dockPanel.ResumeLayout(false);
			this.dockPanel_Container.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.accordionControl)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.tabbedView)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.documentManager)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private DevExpress.XtraBars.Ribbon.RibbonControl ribbonControl;
		private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPageMarket;
		private DevExpress.XtraBars.Ribbon.RibbonStatusBar ribbonStatusBar;
		private DevExpress.XtraBars.Docking.DockManager dockManager;
		private DevExpress.XtraBars.Docking.DockPanel dockPanel;
		private DevExpress.XtraBars.Docking.ControlContainer dockPanel_Container;
		private DevExpress.XtraBars.Navigation.AccordionControl accordionControl;
		private DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView tabbedView;
		private DevExpress.XtraBars.Docking2010.DocumentManager documentManager;
		private DevExpress.LookAndFeel.DefaultLookAndFeel defaultLookAndFeel;
		private DevExpress.XtraSplashScreen.SplashScreenManager splashScreenManager;
	}
}