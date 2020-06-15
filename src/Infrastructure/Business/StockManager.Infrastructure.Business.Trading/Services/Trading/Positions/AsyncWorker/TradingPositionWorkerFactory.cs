using System;
using System.Threading.Tasks;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.AsyncWorker
{
	public class TradingPositionWorkerFactory
	{
		private readonly CandleLoadingService _candleLoadingService;
		private readonly OrderBookLoadingService _orderBookLoadingService;
		private readonly TradingReportsService _tradingReportsService;
		private readonly IMarketPendingPositionAnalysisService _marketPendingPositionAnalysisService;
		private readonly IMarketOpenPositionAnalysisService _marketOpenPositionAnalysisService;
		private readonly ITradingPositionService _tradingPositionService;
		private readonly ConfigurationService _configurationService;

		public TradingPositionWorkerFactory(CandleLoadingService candleLoadingService,
			OrderBookLoadingService orderBookLoadingService,
			TradingReportsService tradingReportsService,
			IMarketPendingPositionAnalysisService marketPendingPositionAnalysisService,
			IMarketOpenPositionAnalysisService marketOpenPositionAnalysisService,
			ITradingPositionService tradingPositionService,
			ConfigurationService configurationService)
		{
			_candleLoadingService = candleLoadingService ?? throw new ArgumentNullException(nameof(candleLoadingService));
			_orderBookLoadingService = orderBookLoadingService ?? throw new ArgumentNullException(nameof(orderBookLoadingService));
			_tradingReportsService = tradingReportsService ?? throw new ArgumentNullException(nameof(tradingReportsService));
			_marketPendingPositionAnalysisService = marketPendingPositionAnalysisService ?? throw new ArgumentNullException(nameof(marketPendingPositionAnalysisService));
			_marketOpenPositionAnalysisService = marketOpenPositionAnalysisService ?? throw new ArgumentNullException(nameof(marketOpenPositionAnalysisService));
			_tradingPositionService = tradingPositionService ?? throw new ArgumentNullException(nameof(tradingPositionService));
			_configurationService = configurationService ?? throw new ArgumentNullException(nameof(_configurationService));
		}

		public async Task<TradingPositionWorker> CreateWorkerWithExistingPosition(TradingPosition tradingPosition, Action<TradingPositionWorker, PositionChangedEventArgs> positionChangedCallback)
		{
			var positionWorker = CreateNewWorker(positionChangedCallback);
			await positionWorker.LoadExistingPosition(tradingPosition);
			return positionWorker;
		}

		public TradingPositionWorker CreateWorkerWithNewPosition(Action<TradingPositionWorker, PositionChangedEventArgs> positionChangedCallback)
		{
			var positionWorker = CreateNewWorker(positionChangedCallback);
			return positionWorker;
		}

		private TradingPositionWorker CreateNewWorker(Action<TradingPositionWorker, PositionChangedEventArgs> positionChangedCallback)
		{
			var worker = new TradingPositionWorker(
				_candleLoadingService,
				_orderBookLoadingService,
				_tradingReportsService,
				_marketPendingPositionAnalysisService,
				_marketOpenPositionAnalysisService,
				_tradingPositionService,
				_configurationService
			);

			worker.PositionChanged += (o, e) => positionChangedCallback((TradingPositionWorker)o, e);

			return worker;
		}
	}
}