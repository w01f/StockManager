using System;
using System.Linq;
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

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions.AsyncWorker
{
	public class TradingPositionWorker
	{
		private readonly CandleLoadingService _candleLoadingService;
		private readonly OrderBookLoadingService _orderBookLoadingService;
		private readonly TradingReportsService _tradingReportsService;
		private readonly IMarketPendingPositionAnalysisService _marketPendingPositionAnalysisService;
		private readonly IMarketOpenPositionAnalysisService _marketOpenPositionAnalysisService;
		private readonly ITradingPositionService _tradingPositionService;

		public TradingPosition Position { get; private set; }

		public event EventHandler<PositionChangedEventArgs> PositionChanged;

		public TradingPositionWorker(CandleLoadingService candleLoadingService,
			OrderBookLoadingService orderBookLoadingService,
			TradingReportsService tradingReportsService,
			IMarketPendingPositionAnalysisService marketPendingPositionAnalysisService,
			IMarketOpenPositionAnalysisService marketOpenPositionAnalysisService,
			ITradingPositionService tradingPositionService)
		{
			_candleLoadingService = candleLoadingService ?? throw new ArgumentNullException(nameof(candleLoadingService));
			_orderBookLoadingService = orderBookLoadingService ?? throw new ArgumentNullException(nameof(orderBookLoadingService));
			_tradingReportsService = tradingReportsService ?? throw new ArgumentNullException(nameof(tradingReportsService));
			_marketPendingPositionAnalysisService = marketPendingPositionAnalysisService ?? throw new ArgumentNullException(nameof(marketPendingPositionAnalysisService));
			_marketOpenPositionAnalysisService = marketOpenPositionAnalysisService ?? throw new ArgumentNullException(nameof(marketOpenPositionAnalysisService));
			_tradingPositionService = tradingPositionService ?? throw new ArgumentNullException(nameof(tradingPositionService));
		}

		public void LoadExistingPosition(TradingPosition existingPosition)
		{
			Position = existingPosition;
			SubscribeOnTradingEvents();
		}

		public async Task CreateNewPosition(NewOrderPositionInfo positionInfo)
		{
			Position = await _tradingPositionService.OpenPosition(positionInfo, OnPositionChanged);
			OnPositionChanged(TradingEventType.NewPosition);
			SubscribeOnTradingEvents();
		}

		private async Task AnalyzePosition()
		{
			if (Position.IsOpenPosition)
			{
				var marketInfo = await _marketOpenPositionAnalysisService.ProcessMarketPosition(Position);
				if (marketInfo.PositionType == OpenMarketPositionType.UpdateOrder)
					Position.ChangePosition((UpdateClosePositionInfo)marketInfo);
				else if (marketInfo.PositionType == OpenMarketPositionType.FixStopLoss)
					Position.ChangePosition((FixStopLossInfo)marketInfo);
				else if (marketInfo.PositionType == OpenMarketPositionType.Suspend)
					Position.ChangePosition((SuspendPositionInfo)marketInfo);

				if (marketInfo.PositionType != OpenMarketPositionType.Hold)
					await _tradingPositionService.UpdatePosition(Position, OnPositionChanged);
			}
			else if (Position.IsPendingPosition)
			{
				var marketInfo = await _marketPendingPositionAnalysisService.ProcessMarketPosition(Position);
				if (marketInfo.PositionType == PendingMarketPositionType.UpdateOrder)
					Position.ChangePosition((UpdateOrderInfo)marketInfo);
				else if (marketInfo.PositionType == PendingMarketPositionType.CancelOrder)
					Position.ChangePosition((CancelOrderInfo)marketInfo);

				if (marketInfo.PositionType != PendingMarketPositionType.Hold)
					await _tradingPositionService.UpdatePosition(Position, OnPositionChanged);
			}
			else
				throw new BusinessException("Unexpected position state")
				{
					Details = $"Order pair: {JsonConvert.SerializeObject(Position)}"
				};
		}

		private async Task UpdatePosition(Order changedOrder)
		{
			var orderIds = new[] { Position.OpenPositionOrder.ClientId, Position.ClosePositionOrder.ClientId, Position.StopLossOrder.ClientId };
			if (orderIds.Contains(changedOrder.ClientId))
			{
				var nextPosition = Position.Clone();
				if (Position.OpenPositionOrder.ClientId == changedOrder.ClientId)
					nextPosition.OpenPositionOrder.SyncWithAnotherOrder(changedOrder);
				else if (Position.ClosePositionOrder.ClientId == changedOrder.ClientId)
					nextPosition.ClosePositionOrder.SyncWithAnotherOrder(changedOrder);
				else if (Position.StopLossOrder.ClientId == changedOrder.ClientId)
					nextPosition.StopLossOrder.SyncWithAnotherOrder(changedOrder);

				nextPosition = await _tradingPositionService.UpdatePosition(Position, nextPosition, OnPositionChanged);
				if (nextPosition != null)
					Position.SyncWithAnotherPosition(nextPosition);
			}
		}

		private async Task UpdateOrderPrice()
		{
			if (Position.OpenPositionOrder.OrderStateType == OrderStateType.New)
			{
				var newPrice = await _orderBookLoadingService.GetTopBidPrice(Position.OpenPositionOrder.CurrencyPair);
				if (newPrice > Position.OpenPositionOrder.Price)
				{
					var nextPosition = Position.Clone();
					nextPosition.OpenPositionOrder.Price = newPrice;
					nextPosition = await _tradingPositionService.UpdatePosition(Position, nextPosition, OnPositionChanged);
					if (nextPosition != null)
						Position.SyncWithAnotherPosition(nextPosition);
				}
			}
		}

		private void SubscribeOnTradingEvents()
		{
			_candleLoadingService.CandlesUpdated += OnCandlesUpdated;
			_orderBookLoadingService.OrderBookUpdated += OnOrderBookUpdated;
			_tradingReportsService.OrdersUpdated += OnOrdersUpdated;
		}

		private void UnsubscribeTradingEvents()
		{
			_candleLoadingService.CandlesUpdated -= OnCandlesUpdated;
			_orderBookLoadingService.OrderBookUpdated -= OnOrderBookUpdated;
			_tradingReportsService.OrdersUpdated -= OnOrdersUpdated;
		}

		private void OnCandlesUpdated(object sender, CandlesUpdatedEventArgs e)
		{
			if (Position.CurrencyPairId != e.CurrencyPairId)
				return;

			AnalyzePosition().Wait();
		}

		private void OnOrderBookUpdated(object sender, OrderBookUpdatedEventArgs e)
		{
			if (Position.CurrencyPairId != e.CurrencyPairId)
				return;

			UpdateOrderPrice().Wait();
		}

		private void OnOrdersUpdated(object sender, TradingReportEventArgs e)
		{
			UpdatePosition(e.ChangedOrder).Wait();
		}

		private void OnPositionChanged(PositionChangedEventArgs eventArgs)
		{
			var positionClosed = eventArgs.EventType == TradingEventType.PositionClosedDueStopLoss || eventArgs.EventType == TradingEventType.PositionClosedSuccessfully;
			if (positionClosed)
				UnsubscribeTradingEvents();
			PositionChanged?.Invoke(this, eventArgs);
			if (positionClosed)
				PositionChanged = null;
		}

		private void OnPositionChanged(TradingEventType eventType)
		{
			OnPositionChanged(new PositionChangedEventArgs(eventType, Position.CurrencyPairId));
		}
	}
}
