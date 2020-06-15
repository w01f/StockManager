using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StockManager.Infrastructure.Connectors.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Rest.Services.HitBtc;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Connectors.Rest.Test
{
	/// <summary>
	/// Summary description for StockTests
	/// </summary>
	[TestClass]
	public class StockTests
	{
		private readonly IStockRestConnector _stockRestConnector = new StockRestConnector(new ConfigurationService());

		[TestMethod]
		public async Task GetCurrenciesReturnsNonEmpty()
		{
			var currencies = await _stockRestConnector.GetCurrencyPairs();
			Assert.IsTrue(currencies.Any());
		}

		[TestMethod]
		public async Task GetCurrencyReturnsNotNull()
		{
			var currencyPair = await _stockRestConnector.GetCurrencyPair("ETHBTC");
			Assert.IsNotNull(currencyPair);
		}

		[TestMethod]
		public async Task GetCurrencyThrowsError()
		{
			await Assert.ThrowsExceptionAsync<ConnectorException>(async () =>
			{
				await _stockRestConnector.GetCurrencyPair("ETH");
			});
		}
	}
}
