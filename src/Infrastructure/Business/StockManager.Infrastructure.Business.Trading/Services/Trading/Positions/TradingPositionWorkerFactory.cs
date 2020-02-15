using System;
using System.Threading.Tasks;
using StockManager.Domain.Core.Entities.Trading;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions
{
	public class TradingPositionWorkerFactory : ITradingPositionWorkerFactory
	{
		private readonly IRepository<Order> _orderRepository;
		private readonly IMarketDataRestConnector _marketDataRestConnector;
		private readonly ITradingDataRestConnector _tradingDataRestConnector;
		private readonly CandleLoadingService _candleLoadingService;
		private readonly ConfigurationService _configurationService;
		private readonly ILoggingService _loggingService;

		public TradingPositionWorkerFactory(IRepository<Order> orderRepository,
			IMarketDataRestConnector marketDataRestConnector,
			ITradingDataRestConnector tradingDataRestConnector,
			CandleLoadingService candleLoadingService,
			ConfigurationService configurationService,
			ILoggingService loggingService)
		{
			_orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
			_marketDataRestConnector = marketDataRestConnector ?? throw new ArgumentNullException(nameof(marketDataRestConnector));
			_tradingDataRestConnector = tradingDataRestConnector ?? throw new ArgumentNullException(nameof(tradingDataRestConnector));
			_candleLoadingService = candleLoadingService ?? throw new ArgumentNullException(nameof(candleLoadingService));
			_configurationService = configurationService ?? throw new ArgumentNullException(nameof(_configurationService));
			_loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
		}

		public TradingPositionWorker CreateWorkerWithExistingPosition(OrderPair orderPair, Action<TradingPositionWorker, PositionChangedEventArgs> positionChangedCallback)
		{
			var positionWorker = CreateNewWorker(positionChangedCallback);
			positionWorker.LoadExistingPosition(orderPair);
			return positionWorker;
		}

		public async Task<TradingPositionWorker> CreateWorkerWithNewPosition(NewOrderPositionInfo newPositionInfo, Action<TradingPositionWorker, PositionChangedEventArgs> positionChangedCallback)
		{
			var positionWorker = CreateNewWorker(positionChangedCallback);
			await positionWorker.CreateNewPosition(newPositionInfo);
			return positionWorker;
		}

		private TradingPositionWorker CreateNewWorker(Action<TradingPositionWorker, PositionChangedEventArgs> positionChangedCallback)
		{
			var worker = new TradingPositionWorker(
				_orderRepository,
				_marketDataRestConnector,
				_tradingDataRestConnector,
				_candleLoadingService,
				_configurationService,
				_loggingService
			);

			worker.PositionChanged += (o, e) => positionChangedCallback((TradingPositionWorker)o, e);

			return worker;
		}
	}
}