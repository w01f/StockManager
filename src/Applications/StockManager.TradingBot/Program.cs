using System;
using System.IO;
using System.Reflection;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Common;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.TradingBot
{
	class Program
	{
		static void Main()
		{
			CompositionRoot.Initialize(new DependencyInitializer());

			var tradingEventsObserver = CompositionRoot.Resolve<TradingEventsObserver>();
			tradingEventsObserver.PositionChanged += OnTradingEventsObserverPositionChanged;

			var configurationService = CompositionRoot.Resolve<ConfigurationService>();
			configurationService.InitializeSettings(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location), "Settings"));

			var tradingController = CompositionRoot.Resolve<ITradingController>();
			tradingController.Exception += (o, e) =>
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Trading failed");
				Console.WriteLine((Exception)e.ExceptionObject);
				Console.ForegroundColor = ConsoleColor.White;
			};
			
			Console.WriteLine("Trading started at {0}", DateTime.Now);
			tradingController.StartTrading();
				
			Console.ReadLine();
		}

		private static void OnTradingEventsObserverPositionChanged(object sender, PositionChangedEventArgs e)
		{
			switch (e.EventType)
			{
				case TradingEventType.NewPosition:
					Console.ForegroundColor = ConsoleColor.Blue;
					Console.WriteLine($"New Position Created {e.Details}");
					break;
				case TradingEventType.PositionOpened:
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine($"Position Opened {e.Details}");
					break;
				case TradingEventType.PositionCancelled:
					Console.ForegroundColor = ConsoleColor.Magenta;
					Console.WriteLine($"Position Cancelled {e.Details}");
					break;
				case TradingEventType.PositionClosedSuccessfully:
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"Position Closed Successfully {e.Details}");
					break;
				case TradingEventType.PositionClosedDueStopLoss:
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"Position Closed Due StopLoss {e.Details}");
					break;
			}
			
			Console.ResetColor();
		}
	}
}
