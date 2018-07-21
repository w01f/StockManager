using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis.Condition
{
	class BullishTrendCondition : BaseCondition
	{
		public override async Task<ConditionCheckingResult> Check(
			TradingSettings settings,
			IRepository<Domain.Core.Entities.Market.Candle> candleRepository,
			IMarketDataConnector marketDataConnector,
			IIndicatorComputingService indicatorComputingService
		)
		{
			var conditionCheckingResult = new ConditionCheckingResult();

			var macdSettings = new MACDSettings
			{
				EMAPeriod1 = 12,
				EMAPeriod2 = 26,
				SignalPeriod = 9
			};

			var candles = (await CandleLoader.Load(
				settings.CurrencyPairId,
				settings.Period.GetHigherFramePeriod(),
				macdSettings.RequiredCandleRangeSize,
				settings.CurrentMoment,
				candleRepository,
				marketDataConnector)).ToList();

			var macdValues = indicatorComputingService.ComputeMACD(
					candles,
					macdSettings.EMAPeriod1,
					macdSettings.EMAPeriod2,
					macdSettings.SignalPeriod)
				.OfType<MACDValue>()
				.ToList();

			var curentMACDValue = macdValues.ElementAtOrDefault(macdValues.Count - 1);

			//if MACD higher then 0
			//if MACD higher then Signal
			//if Histogram higher then 0
			if (!(curentMACDValue != null &&
				  curentMACDValue.MACD.HasValue &&
				  curentMACDValue.Signal.HasValue &&
				  curentMACDValue.Histogram.HasValue &&
				  curentMACDValue.MACD.Value > 0 &&
				  curentMACDValue.MACD.Value > curentMACDValue.Signal.Value &&
				  curentMACDValue.Histogram.Value > 0))
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;

			return conditionCheckingResult;
		}
	}
}
