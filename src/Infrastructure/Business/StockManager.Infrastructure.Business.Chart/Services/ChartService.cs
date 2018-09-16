using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Common;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Chart.Models;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
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
		private readonly IRepository<Domain.Core.Entities.Market.Candle> _candleRepository;
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly IIndicatorComputingService _indicatorComputingService;
		private readonly IMarketNewPositionAnalysisService _marketNewPositionAnalysisService;
		private readonly ConfigurationService _configurationService;

		public ChartService(IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService,
			IMarketNewPositionAnalysisService marketNewPositionAnalysisService,
			ConfigurationService configurationService)
		{
			_candleRepository = candleRepository;
			_marketDataConnector = marketDataConnector;
			_indicatorComputingService = indicatorComputingService;
			_marketNewPositionAnalysisService = marketNewPositionAnalysisService;
			_configurationService = configurationService;
		}

		public async Task<ChartDataset> GetChartData(ChartSettings settings)
		{
			var chartDataset = new ChartDataset();

			chartDataset.Candles = (await CandleLoader.Load(
				settings.CurrencyPairId,
				settings.Period,
				settings.CandleRangeSize,
				settings.CurrentMoment,
				_candleRepository,
				_marketDataConnector)).ToList();

			foreach (var indicatorSettings in settings.Indicators)
			{
				var indicatorDataset = new IndicatorDataset();
				indicatorDataset.Settings = indicatorSettings;

				var candles = indicatorSettings.CandlePeriod != settings.Period ?
					(await CandleLoader.Load(
						settings.CurrencyPairId,
						indicatorSettings.CandlePeriod,
						settings.CandleRangeSize,
						settings.CurrentMoment,
						_candleRepository,
						_marketDataConnector))
					.ToList() :
					chartDataset.Candles;

				switch (indicatorSettings.Type)
				{
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

			var defaultTradindSettings = _configurationService.GetTradingSettings();

			var tradingSettings = _configurationService.GetTradingSettings();
			tradingSettings.CurrencyPairId = settings.CurrencyPairId;
			tradingSettings.Period = settings.Period;
			tradingSettings.Moment = settings.CurrentMoment;
			_configurationService.UpdateTradingSettings(tradingSettings);

			foreach (var candle in chartDataset.Candles)
			{
				tradingSettings.Moment = candle.Moment;

				_configurationService.UpdateTradingSettings(tradingSettings);

				//var newPositionInfo = await _marketNewPositionAnalysisService.ProcessMarketPosition();

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

			_configurationService.UpdateTradingSettings(defaultTradindSettings);

			return chartDataset;
		}
	}
}
