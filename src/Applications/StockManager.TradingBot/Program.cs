using System;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Management;

namespace StockManager.TradingBot
{
	class Program
	{
		static int Main(string[] args)
		{
			CompositionRoot.Initialize(new DependencyInitializer());
			var tradingService = CompositionRoot.Resolve<ManagementService>();

			var result = 0;

			var tradingSettings = new TradingSettings
			{
				CurrencyPairId = "ETHBTC",
				Period = CandlePeriod.Minute5,
				Moment = new DateTime(2018, 03, 23, 12, 0, 0),
				CandleRangeSize = 100,
				BaseOrderSide = OrderSide.Buy
			};

			Task.Run(async () =>
			{
				try
				{
					await tradingService.RunTradingIteration(tradingSettings);
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
