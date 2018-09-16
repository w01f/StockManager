using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Management;
using StockManager.Infrastructure.Utilities.Configuration.Models;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.TradingBot
{
	class Program
	{
		static int Main(string[] args)
		{
			CompositionRoot.Initialize(new DependencyInitializer());

			var configurationService = CompositionRoot.Resolve<ConfigurationService>();

			CompositionRoot.Resolve<ConfigurationService>()
				.InitializeSettings(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Settings"));

			configurationService.UpdateTradingSettings(new TradingSettings
			{
				CurrencyPairId = "ETHBTC",
				Period = CandlePeriod.Minute5,
				Moment = new DateTime(2018, 03, 23, 12, 0, 0),
				BaseOrderSide = OrderSide.Buy
			});

			var tradingService = CompositionRoot.Resolve<ManagementService>();

			var result = 0;

			Task.Run(async () =>
			{
				try
				{
					await tradingService.RunTradingIteration();
					Console.WriteLine("Iteration completed successfully");
					result = 0;
				}
				catch (Exception e)
				{
					Console.WriteLine("Iteration failed");
					Console.WriteLine(e);
					result = 1;
				}
			}).GetAwaiter().GetResult();

			return result;
		}
	}
}
