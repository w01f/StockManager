using Ninject.Modules;
using StockManager.Dashboard.Controllers;
using StockManager.Dashboard.Views;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Analysis.Trady.Services;
using StockManager.Infrastructure.Business.Chart.Services;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Rest.Services;
using StockManager.Infrastructure.Data.SQLite;
using StockManager.Infrastructure.Data.SQLite.Repositories;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Dashboard
{
	class DependencyInitializer : NinjectModule
	{
		public override void Load()
		{
			Bind<ConfigurationService>()
				.ToSelf()
				.InSingletonScope();

			Bind<SQLiteDataContext>()
				.ToSelf()
				.InSingletonScope();

			Bind(typeof(IRepository<>))
				.To(typeof(CommonRepository<>));

			Bind<IMarketDataConnector>()
				.To<MarketDataConnector>();

			Bind<IIndicatorComputingService>()
				.To<TradyIndicatorComputingService>();

			Bind<CandleLoadingService>()
				.ToSelf();

			Bind<IMarketNewPositionAnalysisService>()
				.To<TripleFrameRSIStrategyAnalysisService>();

			Bind<ChartService>()
				.ToSelf();

			Bind<MainController>()
				.ToSelf()
				.InSingletonScope();
			Bind<CurrencyPairController>()
				.ToSelf()
				.InSingletonScope();


			Bind<FormMain>()
				.ToSelf()
				.InSingletonScope();

			Bind<CurrencyPairDashboardControl>().ToSelf();
		}
	}
}
