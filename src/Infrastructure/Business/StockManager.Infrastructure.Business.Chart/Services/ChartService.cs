using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Common;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Chart.Models;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Connectors.Common.Services;
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

		public ChartService(IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService,
			IMarketNewPositionAnalysisService marketNewPositionAnalysisService)
		{
			_candleRepository = candleRepository;
			_marketDataConnector = marketDataConnector;
			_indicatorComputingService = indicatorComputingService;
			_marketNewPositionAnalysisService = marketNewPositionAnalysisService;
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
							candles,
							((CommonIndicatorSettings)indicatorSettings).Period);
						break;
					case IndicatorType.WilliamsR:
						indicatorDataset.Values = _indicatorComputingService.ComputeWilliamsR(
							candles,
							((CommonIndicatorSettings)indicatorSettings).Period);
						break;
					default:
						throw new AnalysisException("Undefined indicator type");
				}
				chartDataset.IndicatorData.Add(indicatorDataset);
			}

			var defaultTradindSettings = new TradingSettings
			{
				CurrencyPairId = settings.CurrencyPairId,
				Period = settings.Period,
				Moment = settings.CurrentMoment,
				CandleRangeSize = settings.CandleRangeSize
			};

			foreach (var candle in chartDataset.Candles)
			{
				var tradingSettings = new TradingSettings().InitializeFromTemplate(defaultTradindSettings);
				tradingSettings.Moment = candle.Moment;
				//var newPositionInfo = await _marketNewPositionAnalysisService.ProcessMarketPosition(tradingSettings);

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

			return chartDataset;
		}
	}
}
