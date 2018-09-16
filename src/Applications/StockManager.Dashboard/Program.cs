using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using StockManager.Dashboard.Views;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Dashboard
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			CompositionRoot.Initialize(new DependencyInitializer());

			CompositionRoot.Resolve<ConfigurationService>()
				.InitializeSettings(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Settings"));

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(CompositionRoot.Resolve<FormMain>());
		}
	}
}
