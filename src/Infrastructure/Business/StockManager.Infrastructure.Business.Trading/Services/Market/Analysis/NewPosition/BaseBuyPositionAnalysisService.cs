using System.Linq;
using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Common.Helpers;
using StockManager.Infrastructure.Business.Trading.Common.Enums;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;

namespace StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.NewPosition
{
	public abstract class BaseBuyPositionAnalysisService : BaseNewPositionAnalysisService, IMarketNewPositionAnalysisService
	{
		public async Task<NewPositionInfo> ProcessMarketPosition(TradingSettings settings)
		{
			NewPositionInfo newPositionInfo;
			var conditionCheckingResult = await CheckConditions(settings);

			switch (conditionCheckingResult.ResultType)
			{
				case ConditionCheckingResultType.Passed:
					var buyPositionInfo = new NewOrderPositionInfo(NewMarketPositionType.Buy);

					var candles = (await CandleLoader.Load(
						settings.CurrencyPairId,
						settings.Period,
						2,
						settings.Moment,
						CandleRepository,
						MarketDataConnector)).ToList();

					//TODO Define stop prices
					buyPositionInfo.Price = candles.Max(candle => candle.MaxPrice);
					buyPositionInfo.StopLossPrice = candles.Min(candle => candle.MinPrice);

					newPositionInfo = buyPositionInfo;
					break;
				default:
					newPositionInfo = new WaitPositionInfo();
					break;
			}

			return newPositionInfo;
		}
	}
}
