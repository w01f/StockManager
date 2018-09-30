using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Domain.Core.Enums;
using StockManager.Domain.Core.Repositories;
using StockManager.Infrastructure.Business.Trading.Helpers;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Common;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Connectors.Fake.Services
{
	public class FakeTradingConnector : ITradingDataConnector
	{
		private readonly IRepository<Domain.Core.Entities.Trading.Order> _orderRepository;
		private readonly IRepository<Domain.Core.Entities.Trading.TradingBallance> _tradingBallanceRepository;
		private readonly CandleLoadingService _candleLoadingService;
		private readonly ConfigurationService _configurationService;

		public FakeTradingConnector(IRepository<Domain.Core.Entities.Trading.Order> orderRepository,
			IRepository<Domain.Core.Entities.Trading.TradingBallance> tradingBallanceRepository,
			CandleLoadingService candleLoadingService,
			ConfigurationService configurationService)
		{
			_orderRepository = orderRepository;
			_tradingBallanceRepository = tradingBallanceRepository;
			_candleLoadingService = candleLoadingService;
			_configurationService = configurationService;
		}

		public async Task<TradingBallance> GetTradingBallnce(string currencyId)
		{
			return await Task.Run(() => GetTradingBallnceInner(currencyId).ToModel());
		}

		private Domain.Core.Entities.Trading.TradingBallance GetTradingBallnceInner(string currencyId)
		{
			var tradingBallance = _tradingBallanceRepository.GetAll()
				.FirstOrDefault(entity => String.Equals(entity.CurrencyId, currencyId, StringComparison.OrdinalIgnoreCase));

			if (tradingBallance == null)
			{
				var settings = _configurationService.GetTradingSettings();

				var tradingBallanceModel = new TradingBallance
				{
					CurrencyId = currencyId,
					Available = settings.QuoteCurrencies.Contains(currencyId) ? 100m : 0m,
					Reserved = 0m
				};
				tradingBallance = tradingBallanceModel.ToEntity();
				_tradingBallanceRepository.Insert(tradingBallance);

				tradingBallance = _tradingBallanceRepository.GetAll()
					.FirstOrDefault(entity => String.Equals(entity.CurrencyId, currencyId, StringComparison.OrdinalIgnoreCase));
			}
			return tradingBallance;
		}

		public async Task<IList<Order>> GetActiveOrders(CurrencyPair currencyPair)
		{
			var storedOrders = _orderRepository.GetAll()
				.Where(orderEntity => String.Equals(orderEntity.CurrencyPair, currencyPair.Id, StringComparison.OrdinalIgnoreCase))
				.ToList();

			if (storedOrders.Any())
			{
				var settings = _configurationService.GetTradingSettings();

				var candle = (await _candleLoadingService.LoadCandles(
						currencyPair.Id,
						settings.Period.GetLowerFramePeriod(),
						1,
						settings.Moment))
					.Single();

				var tradingBallanceQuoteCurrencyEntity = GetTradingBallnceInner(currencyPair.QuoteCurrencyId);
				if (tradingBallanceQuoteCurrencyEntity == null)
					throw new ConnectorException(String.Format("Ballance for currency {0} not found", currencyPair.QuoteCurrencyId));

				var tradingBallanceBaseCurrencyEntity = GetTradingBallnceInner(currencyPair.BaseCurrencyId);
				if (tradingBallanceBaseCurrencyEntity == null)
					throw new ConnectorException(String.Format("Ballance for currency {0} not found", currencyPair.BaseCurrencyId));

				var openPositionOrderEntity = storedOrders.Single(orderEntity => orderEntity.Role == OrderRoleType.OpenPosition);
				switch (openPositionOrderEntity.OrderStateType)
				{
					case OrderStateType.Suspended:
						if (openPositionOrderEntity.StopPrice <= candle.MaxPrice)
						{
							openPositionOrderEntity.OrderStateType = OrderStateType.New;
							openPositionOrderEntity.OrderType = OrderType.Limit;

							tradingBallanceQuoteCurrencyEntity.Reserved = openPositionOrderEntity.Price * openPositionOrderEntity.Quantity;
							tradingBallanceQuoteCurrencyEntity.Available -= openPositionOrderEntity.Price * openPositionOrderEntity.Quantity;

							if (tradingBallanceQuoteCurrencyEntity.Available < 0)
								throw new ConnectorException("Ballance unavailable");

							_orderRepository.Update(openPositionOrderEntity);

							_tradingBallanceRepository.Update(tradingBallanceQuoteCurrencyEntity);
						}
						break;
					case OrderStateType.New:
						if (openPositionOrderEntity.Price >= candle.MinPrice)
						{
							openPositionOrderEntity.OrderStateType = OrderStateType.Filled;

							tradingBallanceQuoteCurrencyEntity.Available += openPositionOrderEntity.Price * openPositionOrderEntity.Quantity * 0.0001m;
							tradingBallanceBaseCurrencyEntity.Available += openPositionOrderEntity.Quantity;
							tradingBallanceQuoteCurrencyEntity.Reserved = 0m;

							_orderRepository.Update(openPositionOrderEntity);

							_tradingBallanceRepository.Update(tradingBallanceQuoteCurrencyEntity);
							_tradingBallanceRepository.Update(tradingBallanceBaseCurrencyEntity);
						}
						break;
					case OrderStateType.Filled:
						var closePositionOrderEntity =
							storedOrders.Single(orderEntity => orderEntity.Role == OrderRoleType.ClosePosition && orderEntity.ParentClientId == openPositionOrderEntity.ClientId);
						switch (closePositionOrderEntity.OrderStateType)
						{
							case OrderStateType.Suspended:
								if (closePositionOrderEntity.StopPrice >= candle.MinPrice)
								{
									closePositionOrderEntity.OrderStateType = OrderStateType.New;
									openPositionOrderEntity.OrderType = OrderType.Limit;

									tradingBallanceBaseCurrencyEntity.Reserved = closePositionOrderEntity.Quantity;
									tradingBallanceBaseCurrencyEntity.Available -= closePositionOrderEntity.Quantity;

									if (tradingBallanceBaseCurrencyEntity.Available < 0)
										throw new ConnectorException("Ballance unavailable");

									_orderRepository.Update(closePositionOrderEntity);

									_tradingBallanceRepository.Update(tradingBallanceBaseCurrencyEntity);
								}
								break;
							case OrderStateType.New:
								if (closePositionOrderEntity.Price <= candle.MaxPrice)
								{
									closePositionOrderEntity.OrderStateType = OrderStateType.Filled;

									tradingBallanceQuoteCurrencyEntity.Available +=
										closePositionOrderEntity.Quantity * closePositionOrderEntity.Price * 1.0001m;

									tradingBallanceBaseCurrencyEntity.Reserved = 0m;

									_orderRepository.Update(closePositionOrderEntity);

									_tradingBallanceRepository.Update(tradingBallanceQuoteCurrencyEntity);
									_tradingBallanceRepository.Update(tradingBallanceBaseCurrencyEntity);
								}
								break;
						}

						if (closePositionOrderEntity.OrderStateType != OrderStateType.Filled)
						{
							var stopLossOrderEntity =
								storedOrders.Single(orderEntity => orderEntity.Role == OrderRoleType.StopLoss && orderEntity.ParentClientId == openPositionOrderEntity.ClientId);
							switch (stopLossOrderEntity.OrderStateType)
							{
								case OrderStateType.Suspended:
									if (stopLossOrderEntity.StopPrice >= candle.MinPrice || stopLossOrderEntity.Price >= candle.MinPrice)
									{
										stopLossOrderEntity.OrderStateType = OrderStateType.Filled;

										tradingBallanceQuoteCurrencyEntity.Available +=
											(stopLossOrderEntity.Quantity * stopLossOrderEntity.StopPrice ?? 0m) * 0.999m;

										if (tradingBallanceBaseCurrencyEntity.Reserved == 0)
											tradingBallanceBaseCurrencyEntity.Available -= stopLossOrderEntity.Quantity;
										tradingBallanceBaseCurrencyEntity.Reserved = 0m;

										_orderRepository.Update(stopLossOrderEntity);

										_tradingBallanceRepository.Update(tradingBallanceQuoteCurrencyEntity);
										_tradingBallanceRepository.Update(tradingBallanceBaseCurrencyEntity);
									}
									break;
							}
						}
						break;
				}
			}

			return _orderRepository.GetAll()
				.Where(orderEntity => String.Equals(orderEntity.CurrencyPair, currencyPair.Id, StringComparison.OrdinalIgnoreCase))
				.Select(orderEntity => orderEntity.ToModel(currencyPair)).ToList();
		}

		public async Task<Order> GetOrderFromHistory(Guid clientOrderId, CurrencyPair currencyPair)
		{
			return await Task.Run(() =>
			{
				return _orderRepository.GetAll()
					.Where(orderEntity => orderEntity.ClientId == clientOrderId)
					.Select(orderEntity => orderEntity.ToModel(currencyPair))
					.FirstOrDefault();
			});
		}

		public async Task<Order> CreateOrder(Order initialOrder)
		{
			if (initialOrder.OrderStateType == OrderStateType.New && initialOrder.OrderType == OrderType.Limit)
			{
				var currencyId = initialOrder.Role == OrderRoleType.OpenPosition
					? initialOrder.CurrencyPair.QuoteCurrencyId
					: initialOrder.CurrencyPair.BaseCurrencyId;

				var tradingBallanceEntity = GetTradingBallnceInner(currencyId);

				if (tradingBallanceEntity == null || tradingBallanceEntity.Available <= 0)
					throw new ConnectorException(String.Format("Ballance is unavailable for {0}", currencyId));

				if (initialOrder.Role == OrderRoleType.OpenPosition)
					tradingBallanceEntity.Reserved = initialOrder.Price * initialOrder.Quantity;
				else
					tradingBallanceEntity.Reserved = initialOrder.Quantity;

				tradingBallanceEntity.Available -= tradingBallanceEntity.Reserved;
				_tradingBallanceRepository.Update(tradingBallanceEntity);
			}
			return await Task.Run(() => initialOrder);
		}

		public async Task<Order> CancelOrder(Order initialOrder)
		{
			var targetOrder = initialOrder.Clone();

			var currencyId = targetOrder.Role == OrderRoleType.OpenPosition
				? targetOrder.CurrencyPair.QuoteCurrencyId
				: targetOrder.CurrencyPair.BaseCurrencyId;

			var tradingBallanceEntity = _tradingBallanceRepository.GetAll()
				.FirstOrDefault(entity => entity.CurrencyId == currencyId);

			if (tradingBallanceEntity == null)
				throw new ConnectorException(String.Format("Ballance for currency {0} not found", currencyId));

			tradingBallanceEntity.Available += tradingBallanceEntity.Reserved;
			tradingBallanceEntity.Reserved = 0;

			_tradingBallanceRepository.Update(tradingBallanceEntity);

			targetOrder.OrderStateType = OrderStateType.Cancelled;

			return await Task.Run(() => targetOrder);
		}
	}
}
