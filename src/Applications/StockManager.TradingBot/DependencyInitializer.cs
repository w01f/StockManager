using Ninject.Modules;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Analysis.Trady.Services;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Services.Trading.Management;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.HitBtc.Rest.Services;
using StockManager.Infrastructure.Data.SQLite;
using StockManager.Infrastructure.Data.SQLite.Repositories;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.TradingBot
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

			Bind<IMarketNewPositionAnalysisService>()
				.To<TripleFrameRSIStrategyAnalysisService>();
			Bind<ManagementService>()
				.ToSelf();
		}
	}
}
