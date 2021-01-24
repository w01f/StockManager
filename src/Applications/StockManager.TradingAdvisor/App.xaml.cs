using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Ninject.Parameters;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.TradingAdvisor.ViewModels;
using StockManager.TradingAdvisor.Views;

namespace StockManager.TradingAdvisor
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		private MainViewModel _mainViewModel;

		[STAThread]
		protected override void OnStartup(StartupEventArgs e)
		{
			try
			{
				CompositionRoot.Initialize(new DependencyInitializer());

				CompositionRoot.Resolve<ConfigurationService>()
					.InitializeSettings(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? throw new InvalidOperationException(), "Settings"));

				var window = new MainWindow();
				_mainViewModel = CompositionRoot.Resolve<MainViewModel>(new ConstructorArgument("dispatcher", Dispatcher));
				window.DataContext = _mainViewModel;

				window.Show();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Trading Advisor");
				Current?.Shutdown(-1);
			}
		}
	}
}
