using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Common;
using StockManager.Infrastructure.Connectors.Socket.Services.HitBtc;

namespace StockManager.Infrastructure.Connectors.Socket.Test
{
	/// <summary>
	/// Summary description for MarketTests
	/// </summary>
	[TestClass]
	public class MarketTests
	{
		private readonly MarketDataSocketConnector _marketDataSocketConnector = new MarketDataSocketConnector();

		[TestMethod]
		public async Task GetCurrenciesReturnsNonEmpty()
		{
			var currencies = await _marketDataSocketConnector.GetCurrencyPairs();
			Assert.IsTrue(currencies.Any());
		}

		[TestMethod]
		public async Task GetCurrencyReturnsNotNull()
		{
			var currencyPair = await _marketDataSocketConnector.GetCurrencyPair("ETHBTC");
			Assert.IsNotNull(currencyPair);
		}

		[TestMethod]
		public async Task GetCurrencyThrowsError()
		{
			await Assert.ThrowsExceptionAsync<ConnectorException>(async () =>
			{
				await _marketDataSocketConnector.GetCurrencyPair("ETH");
			});
		}

		[TestMethod]
		public async Task SubscribeOnCandlesReturnsNonEmpty()
		{
			var receivedCandles = new List<Candle>();
			await _marketDataSocketConnector.SubscribeOnCandles("ETHBTC", CandlePeriod.Minute1, candles =>
			{
				foreach (var candle in candles)
				{
					var existingCandle = receivedCandles.FirstOrDefault(receivedCandle => receivedCandle.Moment == candle.Moment);
					if (existingCandle == null)
						receivedCandles.Add(candle);
					else
						existingCandle.ClosePrice = candle.ClosePrice;
				}
			}, 10);

			await Task.Delay(61 * 1000);

			Assert.AreEqual(11, receivedCandles.Count);
		}
	}
}
