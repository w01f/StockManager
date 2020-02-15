using System;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.Enums;
using StockManager.Infrastructure.Business.Trading.EventArgs;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Business.Trading.Models.Market.Analysis.NewPosition;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Orders;
using StockManager.Infrastructure.Business.Trading.Services.Extensions.Connectors;
using StockManager.Infrastructure.Common.Common;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;
using StockManager.Infrastructure.Utilities.Logging.Common.Enums;
using StockManager.Infrastructure.Utilities.Logging.Models.Orders;
using StockManager.Infrastructure.Utilities.Logging.Services;

namespace StockManager.Infrastructure.Business.Trading.Services.Trading.Positions
{
	public class TradingPositionWorker
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly IMarketDataRestConnector _marketDataRestConnector;
		private readonly ITradingDataRestConnector _tradingDataRestConnector;
		private readonly CandleLoadingService _candleLoadingService;
		private readonly ConfigurationService _configurationService;
		private readonly ILoggingService _loggingService;

		public OrderPair Position { get; private set; }

		public event EventHandler<PositionChangedEventArgs> PositionChanged;

		public TradingPositionWorker(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
			IMarketDataRestConnector marketDataRestConnector,
			ITradingDataRestConnector tradingDataRestConnector,
			CandleLoadingService candleLoadingService,
			ConfigurationService configurationService,
			ILoggingService loggingService)
		{
			_orderRepository = orderRepository;
			_marketDataRestConnector = marketDataRestConnector;
			_tradingDataRestConnector = tradingDataRestConnector;
			_candleLoadingService = candleLoadingService;
			_configurationService = configurationService;
			_loggingService = loggingService;
		}

		public void LoadExistingPosition(OrderPair existingPosition)
		{
			Position = existingPosition;
			SubscribeOnTradingEvents();
		}

		public async Task CreateNewPosition(NewOrderPositionInfo positionInfo)
		{
			await OpenPosition(positionInfo);
			SubscribeOnTradingEvents();
		}

		private async Task OpenPosition(NewOrderPositionInfo positionInfo)
		{
			var tradingSettings = _configurationService.GetTradingSettings();

			var currencyPair = await _marketDataRestConnector.GetCurrensyPair(positionInfo.CurrencyPairId);

			var now = DateTime.UtcNow;

			Position = new OrderPair
			{
				OpenPositionOrder = new Order
				{
					ClientId = Guid.NewGuid(),
					CurrencyPair = currencyPair,
					Role = OrderRoleType.OpenPosition,
					OrderSide = tradingSettings.BaseOrderSide,
					OrderType = OrderType.StopLimit,
					OrderStateType = OrderStateType.Suspended,
					TimeInForce = OrderTimeInForceType.GoodTillCancelled,
					Price = positionInfo.OpenPrice,
					StopPrice = positionInfo.OpenStopPrice,
					Created = now,
					Updated = now
				},

				ClosePositionOrder = new Order
				{
					ClientId = Guid.NewGuid(),
					ParentClientId = Position.OpenPositionOrder.ClientId,
					CurrencyPair = currencyPair,
					Role = OrderRoleType.ClosePosition,
					OrderSide = tradingSettings.OppositeOrderSide,
					OrderType = OrderType.StopLimit,
					OrderStateType = OrderStateType.Pending,
					TimeInForce = OrderTimeInForceType.GoodTillCancelled,
					Price = positionInfo.ClosePrice,
					StopPrice = positionInfo.CloseStopPrice,
					Created = now,
					Updated = now
				},

				StopLossOrder = new Order
				{
					ClientId = Guid.NewGuid(),
					ParentClientId = Position.OpenPositionOrder.ClientId,
					CurrencyPair = currencyPair,
					Role = OrderRoleType.StopLoss,
					OrderSide = tradingSettings.OppositeOrderSide,
					OrderType = OrderType.StopMarket,
					OrderStateType = OrderStateType.Suspended,
					TimeInForce = OrderTimeInForceType.GoodTillCancelled,
					Price = 0,
					StopPrice = positionInfo.StopLossPrice,
					Created = now,
					Updated = now
				}
			};

			var quoteTradingBalance = await _tradingDataRestConnector.GetTradingBallnce(currencyPair.QuoteCurrencyId);
			if (quoteTradingBalance?.Available <= 0)
				throw new BusinessException($"Trading balance is empty or not available: {currencyPair.Id}");

			Order serverSideOpenPositionOrder = null;
			do
			{
				if (serverSideOpenPositionOrder != null)
				{
					Position.OpenPositionOrder.OrderType = OrderType.Limit;
					Position.OpenPositionOrder.OrderStateType = OrderStateType.New;
					Position.OpenPositionOrder.StopPrice = null;

					var nearestBidSupportPrice = await _marketDataRestConnector.GetNearestBidSupportPrice(Position.OpenPositionOrder.CurrencyPair);
					Position.OpenPositionOrder.Price = nearestBidSupportPrice + Position.OpenPositionOrder.CurrencyPair.TickSize;
				}

				Position.OpenPositionOrder.CalculateBuyOrderQuantity(quoteTradingBalance, tradingSettings);
				if (Position.OpenPositionOrder.Quantity == 0)
					break;

				try
				{
					serverSideOpenPositionOrder = await _tradingDataRestConnector.CreateOrder(Position.OpenPositionOrder, true);
				}
				catch (Exception)
				{
					serverSideOpenPositionOrder = null;
				}
			} while (serverSideOpenPositionOrder == null || serverSideOpenPositionOrder.OrderStateType == OrderStateType.Expired);

			if (serverSideOpenPositionOrder != null)
			{
				Position.OpenPositionOrder.SyncWithAnotherOrder(serverSideOpenPositionOrder);

				_orderRepository.Insert(Position.OpenPositionOrder.ToEntity());
				_orderRepository.Insert(Position.ClosePositionOrder.ToEntity());
				_orderRepository.Insert(Position.StopLossOrder.ToEntity());

				_loggingService.LogAction(Position.OpenPositionOrder.ToLogAction(OrderActionType.Create));
				_loggingService.LogAction(Position.ClosePositionOrder.ToLogAction(OrderActionType.Create));
				_loggingService.LogAction(Position.StopLossOrder.ToLogAction(OrderActionType.Create));

				OnPositionChanged(TradingEventType.NewPosition);
			}
			else
				throw new BusinessException($"Error while creating new position occured: {currencyPair.Id}");
		}

		private void ClosePosition()
		{
			UnsubscribeTradingEvents();

			OnPositionChanged(TradingEventType.PositionClosedSuccessfully);

			PositionChanged = null;
		}

		private void SubscribeOnTradingEvents()
		{
			_candleLoadingService.CandlesUpdated += OnCandlesUpdated;
		}

		private void UnsubscribeTradingEvents()
		{
			_candleLoadingService.CandlesUpdated -= OnCandlesUpdated;
		}

		private void OnCandlesUpdated(object sender, CandlesUpdatedEventArgs e)
		{
			if (Position.CurrencyPairId != e.CurrencyPairId)
				return;

			var tradingSettings = _configurationService.GetTradingSettings();
			var targetPeriod = tradingSettings.Period.GetLowerFramePeriod();
			if (targetPeriod != e.Period)
				return;

			//TODO Add candles changes processing
		}

		private void OnPositionChanged(TradingEventType eventType)
		{
			PositionChanged?.Invoke(this, new PositionChangedEventArgs(eventType, $"(Pair: {Position.CurrencyPairId})"));
		}
	}
}
