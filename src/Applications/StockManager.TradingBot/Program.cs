﻿using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Common;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Management;
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
			
			var tradingEventsObserver = CompositionRoot.Resolve<TradingEventsObserver>();
			tradingEventsObserver.PositionChanged += OnTradingEventsObserverPositionChanged;

			CompositionRoot.Resolve<ConfigurationService>()
				.InitializeSettings(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Settings"));

			var tradingService = CompositionRoot.Resolve<ManagementService>();

			var result = 0;
			var now = DateTime.Now;
			var dueDateTimeSpan = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 15).AddMinutes(1) - now;
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
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("Iteration failed");
						Console.WriteLine(exception);
						Console.ForegroundColor = ConsoleColor.White;
					}
				}).GetAwaiter().GetResult();
			},
			null,
			dueDateTimeSpan,
			periodTimeSpan);

			Console.ReadLine();
			return result;
		}

		private static void OnTradingEventsObserverPositionChanged(object sender, TradingEventsObserver.PositionChangedEventArgs e)
		{
			switch (e.EventType)
			{
				case TradingEventType.NewPosition:
					Console.ForegroundColor = ConsoleColor.Blue;
					Console.WriteLine("New Position Created");
					break;
				case TradingEventType.PositionOpened:
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine("Position Opened");
					break;
				case TradingEventType.PositionClosedSuccessfully:
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine("Position Closed Successfully");
					break;
				case TradingEventType.PositionClosedDueStopLoss:
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Position Closed Due StopLoss");
					break;
			}
			Console.ForegroundColor = ConsoleColor.White;
		}
	}
}
