using System;
using System.Windows.Forms;
using StockManager.Dashboard.Views;

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
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(CompositionRoot.Resolve<FormMain>());
		}
	}
}
