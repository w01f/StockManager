using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Socket.Services.Tinkoff;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Connectors.Socket.Test
{
	[TestClass]
	[DeploymentItem(ExchangeConnectionSettingsFilePath, ExchangeConnectionSettingsFolderPath)]
	public class TinkoffStockTests
	{
		private const string ExchangeConnectionSettingsFolderPath = @"Settings\Tinkoff";
		private const string ExchangeConnectionSettingsFilePath = @"Settings\Tinkoff\ExchangeConnection.json";

		private StockSocketConnector _stockSocketConnector;

		[TestInitialize]
		public void Initialize()
		{
			var exchangeConnectionSettingsFilePath = Path.GetFullPath(ExchangeConnectionSettingsFilePath);
			Assert.IsTrue(File.Exists(exchangeConnectionSettingsFilePath));

			var configurationService = new ConfigurationService();
			configurationService.InitializeSettings(Path.GetDirectoryName(exchangeConnectionSettingsFilePath));
			_stockSocketConnector = new StockSocketConnector(configurationService);
		}

		[TestMethod]
		public async Task GetCurrenciesReturnsNonEmpty()
		{
			var currencies = await _stockSocketConnector.GetCurrencyPairs();
			Assert.IsTrue(currencies.Any());
		}

		[TestMethod]
		public async Task GetCurrencyReturnsNotNull()
		{
			var currencyPair = await _stockSocketConnector.GetCurrencyPair("YNDX");
			Assert.IsNotNull(currencyPair);
		}

		[TestMethod]
		public async Task GetCurrencyThrowsError()
		{
			var currencyPair = await _stockSocketConnector.GetCurrencyPair("WrongTicker");
			Assert.IsNull(currencyPair);
		}

		[TestMethod]
		public async Task SubscribeOnCandlesReturnsNonEmpty()
		{
			var currencyPair = await _stockSocketConnector.GetCurrencyPair("YNDX");
			Assert.IsNotNull(currencyPair);

			var receivedCandles = new List<Candle>();
			await _stockSocketConnector.SubscribeOnCandles(currencyPair.Id, CandlePeriod.Minute1, candles =>
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

			Assert.IsTrue(receivedCandles.Count >= 11);
		}
	}
}
