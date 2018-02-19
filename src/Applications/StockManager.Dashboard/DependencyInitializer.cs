using Ninject.Modules;
using StockManager.Dashboard.Controllers;
using StockManager.Dashboard.Views;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Common.Services;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.HitBtc.Rest.Services;
using StockManager.Infrastructure.Data.SQLite;
using StockManager.Infrastructure.Data.SQLite.Repositories;

namespace StockManager.Dashboard
{
	class DependencyInitializer : NinjectModule
	{
		public override void Load()
		{
			Bind<SQLiteDataContext>()
				.ToSelf()
				.InSingletonScope()
				.WithConstructorArgument("connectionString", "Data Source=local_cache.db");

			Bind(typeof(IRepository<>))
				.To(typeof(CommonRepository<>));

			Bind<IMarketDataConnector>()
				.To<MarketDataConnector>();

			Bind<MarketDataService>()
				.ToSelf();

			Bind<MainController>()
				.ToSelf()
				.InSingletonScope();
			Bind<MarketController>()
				.ToSelf()
				.InSingletonScope();


			Bind<FormMain>()
				.ToSelf()
				.InSingletonScope();

			Bind<CurrencyPairDashboardControl>().ToSelf();
		}
	}
}
