using Ninject.Modules;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Analysis.Trady.Services;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Controllers;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Socket.Services.Tinkoff;
using StockManager.Infrastructure.Data.SQLite;
using StockManager.Infrastructure.Data.SQLite.Repositories;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Utilities.Logging.Services;
using StockManager.TradingAdvisor.Controllers;
using StockManager.TradingAdvisor.ViewModels;

namespace StockManager.TradingAdvisor
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

			Bind<SQLiteDataContext>()
				.ToSelf()
				.InSingletonScope();

			Bind(typeof(IRepository<>))
				.To(typeof(CommonRepository<>));

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

			Bind<IMarketNewPositionAnalysisService>()
				.To<TripleFrameRSIStrategyAnalysisService>();

			Bind<TinkoffTradingAdvisorController>()
				.ToSelf();

			Bind<InstrumentsController>()
				.ToSelf()
				.InSingletonScope();

			Bind<MainViewModel>()
				.ToSelf()
				.InSingletonScope();
		}
	}
}
