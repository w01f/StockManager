using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Management;
using StockManager.Infrastructure.Utilities.Configuration.Models;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.TradingBot
{
	class Program
	{
		private static Timer _tradingTimer;

		static int Main(string[] args)
		{
			CompositionRoot.Initialize(new DependencyInitializer());

			var configurationService = CompositionRoot.Resolve<ConfigurationService>();

			CompositionRoot.Resolve<ConfigurationService>()
				.InitializeSettings(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Settings"));

			var tradingService = CompositionRoot.Resolve<ManagementService>();

			var result = 0;
			var now = DateTime.Now;
			var dueDateTimeSpan = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 1).AddMinutes(1) - now;
			var periodTimeSpan = TimeSpan.FromMinutes(1);

			_tradingTimer = new Timer(e =>
			{
				Task.Run(async () =>
				{
					try
					{
						Console.WriteLine("Iteration started at {0}", DateTime.Now);

						var watch = System.Diagnostics.Stopwatch.StartNew();

						var tradingSettings = configurationService.GetTradingSettings();
						tradingSettings.Period = CandlePeriod.Minute5;
						tradingSettings.Moment = DateTime.UtcNow;
						tradingSettings.BaseOrderSide = OrderSide.Buy;

						configurationService.UpdateTradingSettings(tradingSettings);

						await tradingService.RunTradingIteration();

						watch.Stop();
						Console.WriteLine("Iteration completed successfully for {0} s", watch.ElapsedMilliseconds / 1000);

						result = 0;
					}
					catch (Exception exception)
					{
						Console.WriteLine("Iteration failed");
						Console.WriteLine(exception);
					}
				}).GetAwaiter().GetResult();
			},
			null,
			dueDateTimeSpan,
			periodTimeSpan);

			Console.ReadLine();
			return result;
		}
	}
}
