using Ninject.Modules;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Rest.Services.HitBtc;
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

			Bind<IMarketDataRestConnector>()
				.To<MarketDataRestConnector>();

			Bind<ITradingDataRestConnector>()
				.To<TradingDataRestConnector>();

			Bind<CandleLoadingService>()
				.ToSelf();

			Bind<FormMain>()
				.ToSelf()
				.InSingletonScope();
		}
	}
}
