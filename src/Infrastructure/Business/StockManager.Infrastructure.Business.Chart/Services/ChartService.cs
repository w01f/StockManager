using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Common;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Chart.Models;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Chart.Services
{
	public class ChartService
	{
		private readonly IRepository<Domain.Core.Entities.Market.Candle> _candleRepository;
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly IIndicatorComputingService _indicatorComputingService;

		public ChartService(IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService)
		{
			_candleRepository = candleRepository;
			_marketDataConnector = marketDataConnector;
			_indicatorComputingService = indicatorComputingService;
		}

		public async Task<ChartDataset> GetChartData(ChartSettings settings)
		{
			var chartDataset = new ChartDataset();

			chartDataset.Candles = (await CandleLoader.Load(
				settings.CurrencyPairId,
				settings.Period,
				settings.CandleLimit,
				settings.CurrentMoment,
				_candleRepository,
				_marketDataConnector)).ToList();

			foreach (var indicatorSettings in settings.Indicators)
			{
				var indicatorDataset = new IndicatorDataset();
				indicatorDataset.Settings = indicatorSettings;
				switch (indicatorSettings.Type)
				{
					case IndicatorType.EMA:
						indicatorDataset.Values = _indicatorComputingService.ComputeEMA(
							chartDataset.Candles,
							((CommonIndicatorSettings)indicatorSettings).Period);
						break;
					case IndicatorType.MACD:
						indicatorDataset.Values = _indicatorComputingService.ComputeMACD(
							chartDataset.Candles,
							((MACDSettings)indicatorSettings).EMAPeriod1,
							((MACDSettings)indicatorSettings).EMAPeriod2,
							((MACDSettings)indicatorSettings).SignalPeriod);
						break;
					case IndicatorType.Stochastic:
						indicatorDataset.Values = _indicatorComputingService.ComputeStochastic(
							chartDataset.Candles,
							((StochasticSettings)indicatorSettings).Period,
							((StochasticSettings)indicatorSettings).SMAPeriodK,
							((StochasticSettings)indicatorSettings).SMAPeriodD);
						break;
					default:
						throw new AnalysisException("Undefined indicator type");
				}
				chartDataset.IndicatorData.Add(indicatorDataset);
			}

			return chartDataset;
		}
	}
}
