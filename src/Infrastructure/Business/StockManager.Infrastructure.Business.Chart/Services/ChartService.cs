using System.Linq;
using StockManager.Infrastructure.Analysis.Common.Common;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Chart.Models;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using CommonIndicatorSettings = StockManager.Infrastructure.Business.Chart.Models.CommonIndicatorSettings;
using IndicatorType = StockManager.Infrastructure.Business.Chart.Models.IndicatorType;
using MACDSettings = StockManager.Infrastructure.Business.Chart.Models.MACDSettings;
using StochasticSettings = StockManager.Infrastructure.Business.Chart.Models.StochasticSettings;

namespace StockManager.Infrastructure.Business.Chart.Services
{
	public class ChartService
	{
		private readonly CandleLoadingService _candleLoadingService;
		private readonly IIndicatorComputingService _indicatorComputingService;
		private readonly ConfigurationService _configurationService;

		public ChartService(CandleLoadingService candleLoadingService,
			IIndicatorComputingService indicatorComputingService,
			ConfigurationService configurationService)
		{
			_candleLoadingService = candleLoadingService;
			_indicatorComputingService = indicatorComputingService;
			_configurationService = configurationService;
		}

		public ChartDataset GetChartData(ChartSettings settings)
		{
			var chartDataset = new ChartDataset();

			chartDataset.Candles = _candleLoadingService.LoadCandles(
				settings.CurrencyPairId,
				settings.Period,
				settings.CandleRangeSize,
				settings.CurrentMoment).ToList();

			foreach (var indicatorSettings in settings.Indicators)
			{
				var indicatorDataset = new IndicatorDataset();
				indicatorDataset.Settings = indicatorSettings;

				var candles = indicatorSettings.CandlePeriod != settings.Period ?
					_candleLoadingService.LoadCandles(
						settings.CurrencyPairId,
						indicatorSettings.CandlePeriod,
						settings.CandleRangeSize,
						settings.CurrentMoment).ToList() :
					chartDataset.Candles;

				switch (indicatorSettings.Type)
				{
					case IndicatorType.HighestMaxPrice:
						indicatorDataset.Values = _indicatorComputingService.ComputeHighestMaxPrices(
							candles,
							((CommonIndicatorSettings)indicatorSettings).Period);
						break;
					case IndicatorType.EMA:
						indicatorDataset.Values = _indicatorComputingService.ComputeEMA(
							candles,
							((CommonIndicatorSettings)indicatorSettings).Period);
						break;
					case IndicatorType.MACD:
						indicatorDataset.Values = _indicatorComputingService.ComputeMACD(
							candles,
							((MACDSettings)indicatorSettings).EMAPeriod1,
							((MACDSettings)indicatorSettings).EMAPeriod2,
							((MACDSettings)indicatorSettings).SignalPeriod);
						break;
					case IndicatorType.Stochastic:
						indicatorDataset.Values = _indicatorComputingService.ComputeStochastic(
							candles,
							((StochasticSettings)indicatorSettings).Period,
							((StochasticSettings)indicatorSettings).SMAPeriodK,
							((StochasticSettings)indicatorSettings).SMAPeriodD);
						break;
					case IndicatorType.RelativeStrengthIndex:
						indicatorDataset.Values = _indicatorComputingService.ComputeRelativeStrengthIndex(
							candles,
							((CommonIndicatorSettings)indicatorSettings).Period);
						break;
					case IndicatorType.AccumulationDistribution:
						indicatorDataset.Values = _indicatorComputingService.ComputeAccumulationDistribution(
							candles);
						break;
					case IndicatorType.WilliamsR:
						indicatorDataset.Values = _indicatorComputingService.ComputeWilliamsR(
							candles,
							((CommonIndicatorSettings)indicatorSettings).Period);
						break;
					case IndicatorType.ParabolicSAR:
						indicatorDataset.Values = _indicatorComputingService.ComputeParabolicSAR(candles);
						break;
					default:
						throw new AnalysisException("Undefined indicator type");
				}
				chartDataset.IndicatorData.Add(indicatorDataset);
			}

			var defaultTradingSettings = _configurationService.GetTradingSettings();

			var tradingSettings = _configurationService.GetTradingSettings();
			tradingSettings.Period = settings.Period;
			tradingSettings.Moment = settings.CurrentMoment;
			_configurationService.UpdateTradingSettings(tradingSettings);

			foreach (var candle in chartDataset.Candles)
			{
				tradingSettings.Moment = candle.Moment;

				_configurationService.UpdateTradingSettings(tradingSettings);

				//var newPositionInfo = await _marketNewPositionAnalysisService.ProcessMarketPosition(settings.CurrencyPairId);

				var tradingData = new TradingData
				{
					Moment = candle.Moment
				};

				//switch (newPositionInfo.PositionType)
				//{
				//	case NewMarketPositionType.Buy:
				//		tradingData.BuyPrice = candle.ClosePrice;
				//		break;
				//}
				chartDataset.TradingData.Add(tradingData);
			}

			_configurationService.UpdateTradingSettings(defaultTradingSettings);

			return chartDataset;
		}
	}
}
