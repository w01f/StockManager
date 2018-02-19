using Ninject;

namespace StockManager.Dashboard.Controllers
{
	public class MainController
	{
		public MarketController Market { get; }

		[Inject]
		public MainController(MarketController marketController)
		{
			Market = marketController;
		}
	}
}
