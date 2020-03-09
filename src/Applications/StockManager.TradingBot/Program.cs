using System;
using System.IO;
using System.Reflection;
using System.Threading;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Common;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.TradingBot
{
	class Program
	{
		static int Main()
		{
			CompositionRoot.Initialize(new DependencyInitializer());

			var configurationService = CompositionRoot.Resolve<ConfigurationService>();

			var tradingEventsObserver = CompositionRoot.Resolve<TradingEventsObserver>();
			tradingEventsObserver.PositionChanged += OnTradingEventsObserverPositionChanged;

			CompositionRoot.Resolve<ConfigurationService>()
				.InitializeSettings(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location), "Settings"));

			var tradingController = CompositionRoot.Resolve<ITradingController>();

			var result = 0;
			var now = DateTime.Now;
			var dueDateTimeSpan = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 15).AddMinutes(1) - now;
			var periodTimeSpan = TimeSpan.FromMinutes(1);

			var taskComplete = true;
			TimerCallback timerCallback = async e =>
			{
				if (!taskComplete)
					return;

				taskComplete = false;
				try
				{
					Console.WriteLine("Iteration started at {0}", DateTime.Now);

					var watch = System.Diagnostics.Stopwatch.StartNew();

					var tradingSettings = configurationService.GetTradingSettings();
					tradingSettings.Period = CandlePeriod.Minute5;
					tradingSettings.Moment = DateTime.UtcNow;
					tradingSettings.BaseOrderSide = OrderSide.Buy;

					configurationService.UpdateTradingSettings(tradingSettings);

					//await tradingController.StartTrading();

					watch.Stop();
					Console.WriteLine("Iteration completed successfully for {0} s", watch.ElapsedMilliseconds / 1000);

					result = 0;
				}
				catch (Exception exception)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Iteration failed");
					Console.WriteLine(exception);
					Console.ForegroundColor = ConsoleColor.White;
				}
				finally
				{
					taskComplete = true;
				}
			};

			var tradingTimer = new Timer(timerCallback, null, dueDateTimeSpan, periodTimeSpan);
			GC.KeepAlive(tradingTimer);

			Console.ReadLine();
			return result;
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
