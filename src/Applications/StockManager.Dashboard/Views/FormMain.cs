using System;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraBars.Ribbon;
using Ninject;
using Ninject.Parameters;
using StockManager.Dashboard.Controllers;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Dashboard.Views
{
	public partial class FormMain : RibbonForm
	{
		//private readonly MainController _controller;

		public FormMain()
		{
			InitializeComponent();
			ribbonControl.Minimized = true;

			Shown += OnFormShown;
			accordionControl.ElementClick += OnAccordionElementClick;
		}

		[Inject]
		public FormMain(MainController controller) : this()
		{
			//_controller = controller;
		}

		#region Methods
/*
		private async Task LoadCurrencyPairs()
		{
			try
			{
				splashScreenManager.ShowWaitForm();
				accordionControl.Elements.Clear();
				var currencyPairs = await _controller.GetCurrencyPairs();
				if (currencyPairs.Any())
				{
					foreach (var currencyGroup in currencyPairs.GroupBy(currensyPair => new { currensyPair.BaseCurrencyId }))
					{
						var accordionGroup = new AccordionControlElement
						{
							Style = ElementStyle.Group,
							Expanded = false,
							HeaderVisible = true,
							Text = currencyGroup.Key.BaseCurrencyId
						};

						foreach (var currencyPair in currencyGroup)
						{
							var accordionItem = new AccordionControlElement
							{
								Style = ElementStyle.Item,
								Text = currencyPair.Id,
								Tag = currencyPair
							};
							accordionGroup.Elements.Add(accordionItem);
						}
						accordionControl.Elements.Add(accordionGroup);
					}
				}
			}
			finally
			{
				splashScreenManager.CloseWaitForm();
			}
		}
*/
		#endregion

		#region Event Handlers
		private async void OnFormShown(Object sender, EventArgs e)
		{
			//await LoadCurrencyPairs();

			var dashboard = CompositionRoot.Resolve<CurrencyPairDashboardControl>(new ConstructorArgument("info", new CurrencyPair() { Id = "ETHBTC" }));
			tabbedView.AddDocument(dashboard);
			try
			{
				await dashboard.LoadData();
			}
			catch (Exception exception)
			{
				MessageBox.Show(this, exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			tabbedView.ActivateDocument(dashboard);
		}

		private async void OnAccordionElementClick(object sender, ElementClickEventArgs e)
		{
			if (e.Element == null) return;
			if (e.Element.Style == ElementStyle.Group) return;
			if (!(e.Element.Tag is CurrencyPair currencyPair)) return;

			var existedDashboard = tabbedView.Documents.OfType<CurrencyPairDashboardControl>()
				.FirstOrDefault(control => control.Info.Id == currencyPair.Id);
			if (existedDashboard == null)
			{
				existedDashboard = CompositionRoot.Resolve<CurrencyPairDashboardControl>(new ConstructorArgument("info", currencyPair));
				tabbedView.AddDocument(existedDashboard);
				try
				{
					await existedDashboard.LoadData();
				}
				catch (Exception exception)
				{
					MessageBox.Show(this, exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}
			tabbedView.ActivateDocument(existedDashboard);
		}
		#endregion
	}
}