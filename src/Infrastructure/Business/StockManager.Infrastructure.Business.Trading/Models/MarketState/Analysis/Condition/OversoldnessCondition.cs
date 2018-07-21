using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Analysis.Common.Helpers;
using StockManager.Infrastructure.Analysis.Common.Models;
using StockManager.Infrastructure.Analysis.Common.Services;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;

namespace StockManager.Infrastructure.Business.Trading.Models.MarketState.Analysis.Condition
{
	class OversoldnessCondition : BaseCondition
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

			var rsiSettings = new CommonIndicatorSettings()
			{
				Period = 14
			};

			var candleRangeSize = new[] { macdSettings.RequiredCandleRangeSize, rsiSettings.RequiredCandleRangeSize }.Max();

			var candles = (await CandleLoader.Load(
				settings.CurrencyPairId,
				settings.Period,
				candleRangeSize,
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

			var rsiValues = indicatorComputingService.ComputeRelativeStrengthIndex(
					candles,
					rsiSettings.Period)
				.OfType<SimpleIndicatorValue>()
				.ToList();

			var curentMACDValue = macdValues.ElementAtOrDefault(macdValues.Count - 1);

			var curentRSIValue = rsiValues.ElementAtOrDefault(rsiValues.Count - 1);
			var previousRSIValue = rsiValues.ElementAtOrDefault(rsiValues.Count - 2);

			if (curentMACDValue == null || curentRSIValue == null || previousRSIValue == null)
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			//if RSI turning from minimum 
			//if Prev RSI lower then 50
			if (!(curentRSIValue.Value.HasValue &&
				  previousRSIValue.Value.HasValue &&
				  curentRSIValue.Value > previousRSIValue.Value &&
				  previousRSIValue.Value < 50))
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			//If Histogram is lower then avg minimum
			var itemsCount = candles.Count;
			var avgHistogramMinimum = macdValues
				.Where(value => value.Histogram.HasValue)
				.Select(value => value.Histogram.Value)
				.ToList()
				.GetAverageMinimum();
			if (!(curentMACDValue.Histogram.HasValue &&
				  macdValues.Skip(itemsCount - 5).Min(value => value.Histogram) < avgHistogramMinimum))
			{
				conditionCheckingResult.ResultType = ConditionCheckingResultType.Failed;
				return conditionCheckingResult;
			}

			conditionCheckingResult.ResultType = ConditionCheckingResultType.Passed;

			return conditionCheckingResult;
		}
	}
}
