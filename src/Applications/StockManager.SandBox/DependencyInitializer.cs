using Ninject.Modules;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Rest.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.SandBox
{
	class DependencyInitializer : NinjectModule
	{
		public override void Load()
		{
			Bind<ConfigurationService>()
				.ToSelf()
				.InSingletonScope();

			Bind<IMarketDataConnector>()
				.To<MarketDataConnector>();

			Bind<ITradingDataConnector>()
				.To<TradingDataConnector>();

			Bind<CandleLoadingService>()
				.ToSelf();

			Bind<FormMain>()
				.ToSelf()
				.InSingletonScope();
		}
	}
}
