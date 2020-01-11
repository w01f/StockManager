using System;
using System.IO;
using Newtonsoft.Json;
using StockManager.Infrastructure.Utilities.Configuration.Models;

namespace StockManager.Infrastructure.Utilities.Configuration.Services
{
	public class ConfigurationService
	{
		private const string TradingSettingsFileName = "Trading.json";
		private const string AnalysisSettingsFileName = "Analysis.json";
		private const string ExchangeConnectionSettingsFileName = "ExchangeConnection.json";
		private const string DatabaseConnectionSettingsFileName = "DatabaseConnection.json";

		private TradingSettings _tradingSettings;
		private AnalysisSettings _analysisSettings = new AnalysisSettings();
		private ExchangeConnectionSettings _exchangeConnectionSettings;
		private DatabaseConnectionSettings _databaseConnectionSettings;

		public void InitializeSettings(string settingsFolderPath)
		{
			var tradingSettingsFilePath = Path.Combine(settingsFolderPath, TradingSettingsFileName);
			if (File.Exists(tradingSettingsFilePath))
				_tradingSettings = JsonConvert.DeserializeObject<TradingSettings>(File.ReadAllText(tradingSettingsFilePath));

			var analysisSettingsFilePath = Path.Combine(settingsFolderPath, AnalysisSettingsFileName);
			if (File.Exists(analysisSettingsFilePath))
				_analysisSettings = JsonConvert.DeserializeObject<AnalysisSettings>(File.ReadAllText(analysisSettingsFilePath));

			var exchangeConnectionSettingsFilePath = Path.Combine(settingsFolderPath, ExchangeConnectionSettingsFileName);
			if (File.Exists(exchangeConnectionSettingsFilePath))
				_exchangeConnectionSettings = JsonConvert.DeserializeObject<ExchangeConnectionSettings>(File.ReadAllText(exchangeConnectionSettingsFilePath));

			var databaseConnectionSettingsFilePath = Path.Combine(settingsFolderPath, DatabaseConnectionSettingsFileName);
			if (File.Exists(databaseConnectionSettingsFilePath))
				_databaseConnectionSettings = JsonConvert.DeserializeObject<DatabaseConnectionSettings>(File.ReadAllText(databaseConnectionSettingsFilePath));
		}

		public TradingSettings GetTradingSettings()
		{
			if (_tradingSettings == null)
				throw new ArgumentNullException(nameof(_tradingSettings));
			return JsonConvert.DeserializeObject<TradingSettings>(JsonConvert.SerializeObject(_tradingSettings));
		}

		public void UpdateTradingSettings(TradingSettings newSettings)
		{
			if (_tradingSettings == null)
				throw new ArgumentNullException(nameof(_tradingSettings));

			_tradingSettings.QuoteCurrencies = newSettings.QuoteCurrencies;
			_tradingSettings.Period = newSettings.Period;
			_tradingSettings.BaseOrderSide = newSettings.BaseOrderSide;
			_tradingSettings.Moment = newSettings.Moment;

			_tradingSettings.MinCurrencyPairTradingVolumeInBTC = newSettings.MinCurrencyPairTradingVolumeInBTC;
			_tradingSettings.MaxOrderUsingBalancePart = newSettings.MaxOrderUsingBalancePart;
		}

		public AnalysisSettings GetAnalysisSettings()
		{
			if (_analysisSettings == null)
				throw new ArgumentNullException(nameof(_analysisSettings));
			return JsonConvert.DeserializeObject<AnalysisSettings>(JsonConvert.SerializeObject(_analysisSettings));
		}

		public ExchangeConnectionSettings GetExchangeConnectionSettings()
		{
			if (_exchangeConnectionSettings == null)
				throw new ArgumentNullException(nameof(_exchangeConnectionSettings));
			return JsonConvert.DeserializeObject<ExchangeConnectionSettings>(JsonConvert.SerializeObject(_exchangeConnectionSettings));
		}

		public DatabaseConnectionSettings GetDatabaseConnectionSettings()
		{
			if (_databaseConnectionSettings == null)
				throw new ArgumentNullException(nameof(_databaseConnectionSettings));
			return JsonConvert.DeserializeObject<DatabaseConnectionSettings>(JsonConvert.SerializeObject(_databaseConnectionSettings));
		}
	}
}
