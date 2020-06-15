using Ninject.Modules;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Analysis.Trady.Services;
using StockManager.Infrastructure.Business.Collector.Services;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Rest.Services.HitBtc;
using StockManager.Infrastructure.Data.SQLite;
using StockManager.Infrastructure.Data.SQLite.Repositories;

namespace StockManager.BackTestDataCollector
{
	class DependencyInitializer : NinjectModule
	{
		public override void Load()
		{
			Bind<SQLiteDataContext>()
				.ToSelf()
				.InSingletonScope()
				.WithConstructorArgument("connectionString", "Data Source=d:\\home\\data\\sqlite\\local_cache.db");

			Bind(typeof(IRepository<>))
				.To(typeof(CommonRepository<>));

			Bind<IStockRestConnector>()
				.To<StockRestConnector>();

			Bind<IIndicatorComputingService>()
				.To<TradyIndicatorComputingService>();

			Bind<CandleLoadingService>()
				.ToSelf();

			Bind<CollectorService>()
				.ToSelf();
		}
	}
}
