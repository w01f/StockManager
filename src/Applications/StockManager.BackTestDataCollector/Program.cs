using StockManager.Infrastructure.Business.Collector.Services;
using System;
using System.Threading.Tasks;

namespace StockManager.BackTestDataCollector
{
	class Program
	{
		static int Main(string[] args)
		{
			CompositionRoot.Initialize(new DependencyInitializer());
			var collectorService = CompositionRoot.Resolve<CollectorService>();

			var result = 0;

			Task.Run(async () =>
			{
				try
				{
					await collectorService.LoadMaketData("ETHBTC", 11);
					await collectorService.LoadMaketData("BCHBTC", 11);
					await collectorService.LoadMaketData("BCHETH", 11);
					Console.WriteLine("Data loaded successfully");
					result = 0;
				}
				catch (Exception e)
				{
					Console.WriteLine("Data loading failed");
					Console.WriteLine(e);
					result = 1;
				}
			}).GetAwaiter().GetResult();

			return result;
		}
	}
}
