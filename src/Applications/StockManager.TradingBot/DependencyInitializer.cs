using Ninject.Modules;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Analysis.Trady.Services;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Common;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Positions;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Rest.Services.HitBtc;
using StockManager.Infrastructure.Data.SQLite;
using StockManager.Infrastructure.Data.SQLite.Repositories;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.TradingBot
{
	class DependencyInitializer : NinjectModule
	{
		public override void Load()
		{
			Bind<ConfigurationService>()
				.ToSelf()
				.InSingletonScope();
			Bind<ILoggingService>()
				.To<LoggingService>();
			Bind<TradingEventsObserver>()
				.ToSelf()
				.InSingletonScope();

			Bind<SQLiteDataContext>()
				.ToSelf()
				.InSingletonScope();

			Bind(typeof(IRepository<>))
				.To(typeof(CommonRepository<>));

			Bind<IMarketDataRestConnector>()
				.To<MarketDataRestConnector>();
			Bind<ITradingDataConnector>()
				.To<TradingDataConnector>();

			Bind<IIndicatorComputingService>()
				.To<TradyIndicatorComputingService>();

			Bind<CandleLoadingService>()
				.ToSelf()
				.InSingletonScope();

			Bind<IMarketNewPositionAnalysisService>()
				.To<TripleFrameWilliamRStrategyAnalysisService>();
			Bind<IMarketPendingPositionAnalysisService>()
				.To<PendingPositionWilliamsRAnalysisService>();
			Bind<IMarketOpenPositionAnalysisService>()
				.To<OpenPositionAnalysisService>();

			Bind<ITradingPositionService>()
				.To<TradingPositionService>();

			Bind<ITradingController>()
				.To<WebAPITradingController>();
		}
	}
}
