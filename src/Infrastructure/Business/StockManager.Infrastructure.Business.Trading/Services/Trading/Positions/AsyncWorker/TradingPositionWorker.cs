using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Positions;
using StockManager.Infrastructure.Business.Trading.Services.Extensions.Connectors;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.OpenPosition;
using StockManager.Infrastructure.Business.Trading.Services.Market.Analysis.PendingPosition;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.AsyncWorker
{
	public class TradingPositionWorker
	{
		private readonly SemaphoreSlim _positionUpdatingSemaphore = new SemaphoreSlim(1, 1);
		private long _positionUpdatingTasksCount;

		private readonly CandleLoadingService _candleLoadingService;
		private readonly OrderBookLoadingService _orderBookLoadingService;
		private readonly TradingReportsService _tradingReportsService;
		private readonly IMarketPendingPositionAnalysisService _marketPendingPositionAnalysisService;
		private readonly IMarketOpenPositionAnalysisService _marketOpenPositionAnalysisService;
		private readonly ITradingPositionService _tradingPositionService;
		private readonly ConfigurationService _configurationService;

		private CandlePeriod _workingCandlePeriod;

		public TradingPosition Position { get; private set; }

		public event EventHandler<PositionChangedEventArgs> PositionChanged;

		public TradingPositionWorker(CandleLoadingService candleLoadingService,
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
			_configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
		}

		public async Task LoadExistingPosition(TradingPosition existingPosition)
		{
			Position = existingPosition;
			await SubscribeOnTradingEvents();
		}

		public async Task CreateNewPosition(NewOrderPositionInfo positionInfo)
		{
			Position = await _tradingPositionService.OpenPosition(positionInfo);
			Position = await _tradingPositionService.UpdatePosition(null, Position, true, OnPositionChanged);
			await SubscribeOnTradingEvents();
		}

		private async Task SubscribeOnTradingEvents()
		{
			_workingCandlePeriod = _configurationService.GetTradingSettings().Period;
			var tradingSettings = _configurationService.GetTradingSettings();
			var periodsForAnalysis = new[]
			{
				tradingSettings.Period.GetLowerFramePeriod(),
				tradingSettings.Period,
				tradingSettings.Period.GetHigherFramePeriod()
			};
			await _candleLoadingService.InitSubscription(Position.CurrencyPairId, periodsForAnalysis);
			_candleLoadingService.CandlesUpdated += OnCandlesUpdated;

			await _orderBookLoadingService.InitSubscription(Position.CurrencyPair.Id);
			_orderBookLoadingService.OrderBookUpdated += OnOrderBookUpdated;

			await _tradingReportsService.InitSubscription(Position.CurrencyPairId);
			_tradingReportsService.OrdersUpdated += OnOrdersUpdated;
		}

		private void UnsubscribeTradingEvents()
		{
			_candleLoadingService.CandlesUpdated -= OnCandlesUpdated;
			_orderBookLoadingService.OrderBookUpdated -= OnOrderBookUpdated;
			_tradingReportsService.OrdersUpdated -= OnOrdersUpdated;
		}

		private async void OnCandlesUpdated(object sender, CandlesUpdatedEventArgs e)
		{
			await _positionUpdatingSemaphore.WaitAsync();
			try
			{
				if (e.Period != _workingCandlePeriod || Position.CurrencyPairId != e.CurrencyPairId)
					return;

				if (Position.IsAwaitingOrderUpdating)
					return;

				if (Position.IsCompletedPosition)
					return;

				var nextPosition = Position.Clone();
				if (Position.IsOpenPosition)
				{
					var marketInfo = await _marketOpenPositionAnalysisService.ProcessMarketPosition(Position);
					if (marketInfo.PositionType == OpenMarketPositionType.UpdateOrder)
						nextPosition.ChangePosition((UpdateClosePositionInfo)marketInfo);
					else if (marketInfo.PositionType == OpenMarketPositionType.FixStopLoss)
						nextPosition.ChangePosition((FixStopLossInfo)marketInfo);
					else if (marketInfo.PositionType == OpenMarketPositionType.Suspend)
						nextPosition.ChangePosition((SuspendPositionInfo)marketInfo);

					if (marketInfo.PositionType != OpenMarketPositionType.Hold)
					{
						var updatedPosition = await _tradingPositionService.UpdatePosition(Position, nextPosition, true, OnPositionChanged);
						if (updatedPosition != null)
							Position.SyncWithAnotherPosition(updatedPosition, true);
					}
				}
				else if (Position.IsPendingPosition)
				{
					var marketInfo = await _marketPendingPositionAnalysisService.ProcessMarketPosition(Position);
					if (marketInfo.PositionType == PendingMarketPositionType.UpdateOrder)
						nextPosition.ChangePosition((UpdateOrderInfo)marketInfo);
					else if (marketInfo.PositionType == PendingMarketPositionType.CancelOrder)
						nextPosition.ChangePosition((CancelOrderInfo)marketInfo);

					if (marketInfo.PositionType != PendingMarketPositionType.Hold)
					{
						var updatedPosition = await _tradingPositionService.UpdatePosition(Position, nextPosition, true, OnPositionChanged);
						if (updatedPosition != null)
							Position.SyncWithAnotherPosition(updatedPosition, true);
					}
				}
				else
					throw new BusinessException("Unexpected position state")
					{
						Details = $"Order pair: {JsonConvert.SerializeObject(Position)}"
					};
			}
			finally
			{
				_positionUpdatingSemaphore.Release();
			}
		}

		private async void OnOrderBookUpdated(object sender, OrderBookUpdatedEventArgs e)
		{
			if (Position.CurrencyPairId != e.CurrencyPairId)
				return;

			if (Position.IsAwaitingOrderUpdating)
				return;

			if (Interlocked.Read(ref _positionUpdatingTasksCount) > 0)
				return;

			Interlocked.Increment(ref _positionUpdatingTasksCount);

			await _positionUpdatingSemaphore.WaitAsync();
			try
			{
				if (!Position.IsCompletedPosition)
				{
					if (Position.OpenPositionOrder.OrderStateType == OrderStateType.New)
					{
						var newPrice = await _orderBookLoadingService.GetTopBidPrice(Position.OpenPositionOrder.CurrencyPair, 3);
						if (newPrice > Position.OpenPositionOrder.Price)
						{
							var nextPosition = Position.Clone();
							nextPosition.OpenPositionOrder.Price = newPrice;
							nextPosition = await _tradingPositionService.UpdatePosition(Position, nextPosition, true, OnPositionChanged);
							if (nextPosition != null)
								Position.SyncWithAnotherPosition(nextPosition, true);
						}
					}
					else if (Position.ClosePositionOrder.OrderStateType == OrderStateType.New)
					{
						var newPrice = await _orderBookLoadingService.GetBottomAskPrice(Position.OpenPositionOrder.CurrencyPair, 3);
						if (newPrice < Position.ClosePositionOrder.Price)
						{
							var nextPosition = Position.Clone();
							nextPosition.ClosePositionOrder.Price = newPrice;
							nextPosition = await _tradingPositionService.UpdatePosition(Position, nextPosition, true, OnPositionChanged);
							if (nextPosition != null)
								Position.SyncWithAnotherPosition(nextPosition, true);
						}
					}
				}
			}
			finally
			{
				_positionUpdatingSemaphore.Release();
			}

			Interlocked.Decrement(ref _positionUpdatingTasksCount);
		}

		private async void OnOrdersUpdated(object sender, TradingReportEventArgs e)
		{
			await _positionUpdatingSemaphore.WaitAsync();
			try
			{
				if (Position.IsCompletedPosition)
					return;

				var changedOrder = e.ChangedOrder;
				var orderIds = new[] { Position.OpenPositionOrder.ClientId, Position.ClosePositionOrder.ClientId, Position.StopLossOrder.ClientId };
				if (orderIds.Contains(changedOrder.ClientId))
				{
					var nextPosition = Position.Clone();
					Order targetOrder = null;
					if (Position.OpenPositionOrder.ClientId == changedOrder.ClientId)
						targetOrder = nextPosition.OpenPositionOrder;
					else if (Position.ClosePositionOrder.ClientId == changedOrder.ClientId)
						targetOrder = nextPosition.ClosePositionOrder;
					else if (Position.StopLossOrder.ClientId == changedOrder.ClientId)
						targetOrder = nextPosition.StopLossOrder;

					if (targetOrder != null && (Position.IsAwaitingOrderUpdating || targetOrder.OrderStateType != changedOrder.OrderStateType))
					{
						if (targetOrder.OrderStateType == OrderStateType.Suspended &&
							targetOrder.OrderType == OrderType.StopLimit &&
							changedOrder.OrderStateType == OrderStateType.New)
						{
							changedOrder.OrderType = OrderType.Limit;
							changedOrder.StopPrice = null;
						}

						if (changedOrder.OrderStateType == OrderStateType.Expired)
							changedOrder.OrderStateType = OrderStateType.Cancelled;

						targetOrder.SyncWithAnotherOrder(changedOrder);

						nextPosition = await _tradingPositionService.UpdatePosition(Position, nextPosition, false, OnPositionChanged);
						if (nextPosition != null)
							Position.SyncWithAnotherPosition(nextPosition, true);
					}

					Position.IsAwaitingOrderUpdating = false;
				}
			}
			finally
			{
				_positionUpdatingSemaphore.Release();
			}
		}

		private void OnPositionChanged(PositionChangedEventArgs eventArgs)
		{
			if (eventArgs.Position.IsClosedPosition)
			{
				Position.SyncWithAnotherPosition(eventArgs.Position);
				UnsubscribeTradingEvents();
			}

			PositionChanged?.Invoke(this, eventArgs);
			if (eventArgs.Position.IsClosedPosition)
				PositionChanged = null;
		}
	}
}
