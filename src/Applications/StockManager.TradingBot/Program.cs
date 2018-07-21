using System;
using System.Threading.Tasks;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Trading;
using StockManager.Infrastructure.Business.Trading.Services;

namespace StockManager.TradingBot
{
	class Program
	{
		static int Main(string[] args)
		{
			CompositionRoot.Initialize(new DependencyInitializer());
			var tradingService = CompositionRoot.Resolve<TradingService>();

			var result = 0;

			var tradingSettings = new TradingSettings
			{
				CurrencyPairId = "ETHBTC",
				Period = CandlePeriod.Minute5,
				CurrentMoment = new DateTime(2018, 03, 23, 12, 0, 0),
				CandleRangeSize = 100
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
