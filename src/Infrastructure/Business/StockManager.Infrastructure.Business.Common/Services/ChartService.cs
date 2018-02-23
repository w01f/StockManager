using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Common.Models.Chart;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Common.Services
{
	public class ChartService
	{
		private readonly IRepository<Domain.Core.Entities.Market.Candle> _candleRepository;
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly IAnalysisService _analysisService;

		public ChartService(IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IAnalysisService analysisService)
		{
			_candleRepository = candleRepository;
			_marketDataConnector = marketDataConnector;
			_analysisService = analysisService;
		}

		public async Task<ChartDataset> GetChartData(ChartSettings settings)
		{
			var chartDataset = new ChartDataset();

			chartDataset.Candles = (await CandleLoader.Load(
				settings.CurrencyPairId,
				settings.Period,
				settings.CandleLimit,
				_candleRepository,
				_marketDataConnector)).ToList();

			foreach (var indicatorSettings in settings.Indicators)
			{
				var indicatorDataset = new IndicatorDataset();
				indicatorDataset.Settings = indicatorSettings;
				switch (indicatorSettings.Type)
				{
					case IndicatorType.EMA:
						indicatorDataset.Values = _analysisService.ComputeEMA(chartDataset.Candles, indicatorSettings.Period);
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
