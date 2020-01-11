using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.SandBox
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
				.InitializeSettings(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? throw new InvalidOperationException(), "Settings"));

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(CompositionRoot.Resolve<FormMain>());
		}
	}
}
