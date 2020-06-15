using Ninject.Modules;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Analysis.Trady.Services;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Common;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Orders;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.AsyncWorker;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.Workflow;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Rest.Services.HitBtc;
using StockManager.Infrastructure.Connectors.Socket.Services.HitBtc;
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
				.ToSelf();

			Bind(typeof(IRepository<>))
				.To(typeof(CommonRepository<>))
				.InSingletonScope();

			Bind<IStockRestConnector>()
				.To<StockRestConnector>();
			Bind<IStockSocketConnector>()
				.To<StockSocketConnector>()
				.InSingletonScope();

			Bind<IIndicatorComputingService>()
				.To<TradyIndicatorComputingService>();

			Bind<CandleLoadingService>()
				.ToSelf()
				.InSingletonScope();
			Bind<OrderBookLoadingService>()
				.ToSelf()
				.InSingletonScope();
			Bind<TradingReportsService>()
				.ToSelf()
				.InSingletonScope();

			Bind<IMarketNewPositionAnalysisService>()
				.To<TripleFrameRSIStrategyAnalysisService>();
			Bind<IMarketPendingPositionAnalysisService>()
				.To<PendingPositionAnalysisService>();
			Bind<IMarketOpenPositionAnalysisService>()
				.To<OpenPositionAnalysisService>();

			Bind<IOrdersService>()
				.To<OrdersService>();
			Bind<TradingPositionWorkerFactory>()
				.ToSelf()
				.InSingletonScope();
			Bind<TradingWorkflowManager>()
				.ToSelf()
				.InSingletonScope();
			Bind<ITradingPositionService>()
				.To<TradingPositionService>();

			Bind<ITradingController>()
				.To<SocketTradingController>();
		}
	}
}
