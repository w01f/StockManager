using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.Socket.Connection;
using StockManager.Infrastructure.Connectors.Socket.Models.RequestParameters;

namespace StockManager.Infrastructure.Connectors.Socket.Services.HitBtc
{
	public class TradingDataConnector : ITradingDataConnector
	{
		private readonly HitBtcConnection _connection;

		public TradingDataConnector()
		{
			_connection = new HitBtcConnection();
		}

		public Task<TradingBallance> GetTradingBalance(string currencyId)
		{
			throw new NotImplementedException();
		}

		public Task<IList<Order>> GetActiveOrders(CurrencyPair currencyPair)
		{
			throw new NotImplementedException();
		}

		public Task<Order> GetOrderFromHistory(Guid clientOrderId, CurrencyPair currencyPair)
		{
			throw new NotImplementedException();
		}

		public Task<Order> CreateOrder(Order initialOrder, bool usePostOnly)
		{
			throw new NotImplementedException();
		}

		public Task<Order> CancelOrder(Order initialOrder)
		{
			throw new NotImplementedException();
		}

		public async Task SubscribeOrders(IList<CurrencyPair> targetCurrencyPairs, Action<Order> callback)
		{
			var request = new SocketSubscriptionRequest<EmptyRequestParameters>
			{
				RequestMethodName = "subscribeReports",
				NotificationMethodNames =
				{
					"report"
				},
				RequestParameters = new EmptyRequestParameters()
			};

			await _connection.Subscribe<Models.Trading.Order>(request, order =>
			{
				var currencyPair = targetCurrencyPairs.FirstOrDefault(item => String.Equals(item.Id, order.CurrencyPairId, StringComparison.OrdinalIgnoreCase));

				var result = Models.Trading.OrderMap.ToOuterModel(order, currencyPair);

				callback(result);
			});
		}
	}
}
