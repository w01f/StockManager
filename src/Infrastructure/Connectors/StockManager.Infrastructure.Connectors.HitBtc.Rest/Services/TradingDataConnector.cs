using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Services;
using StockManager.Infrastructure.Connectors.HitBtc.Rest.Connection;
using StockManager.Infrastructure.Connectors.HitBtc.Rest.Models.Trading;
using StockManager.Infrastructure.Utilities.Configuration.Services;

namespace StockManager.Infrastructure.Connectors.HitBtc.Rest.Services
{
	public class TradingDataConnector : ITradingDataConnector
	{
		private readonly ConfigurationService _configurationService;

		public TradingDataConnector(ConfigurationService configurationService)
		{
			_configurationService = configurationService;
		}

		public async Task<Infrastructure.Common.Models.Trading.TradingBallance> GetTradingBallnce(string currencyId)
		{
			var connection = new ApiConnection();
			var request = new RestRequest("trading/balance", Method.GET);
			request.Configure();

			var exchangeConnectionSettings = _configurationService.GetExchangeConnectionSettings();

			var response = await connection.DoRequest(
				request,
				exchangeConnectionSettings.ApiKey,
				exchangeConnectionSettings.SecretKey);

			var tradingBallance = response
				.ExtractData<TradingBallance[]>()
				.Where(entity => String.Equals(entity.CurrencyId, currencyId, StringComparison.OrdinalIgnoreCase))
				.Select(entity => entity.ToOuterModel())
				.FirstOrDefault();
			return tradingBallance;
		}

		public async Task<IList<Infrastructure.Common.Models.Trading.Order>> GetActiveOrders(CurrencyPair currencyPair)
		{
			var connection = new ApiConnection();
			var request = new RestRequest("order", Method.GET);
			request.Configure();

			var exchangeConnectionSettings = _configurationService.GetExchangeConnectionSettings();

			var response = await connection.DoRequest(
				request,
				exchangeConnectionSettings.ApiKey,
				exchangeConnectionSettings.SecretKey);

			var orders = response
				.ExtractData<Order[]>()
				.Where(order => order.CurrencyPairId == currencyPair.Id)
				.Select(entity => entity.ToOuterModel(currencyPair))
				.ToList();

			return orders;
		}

		public async Task<Infrastructure.Common.Models.Trading.Order> GetOrderFromHistory(Guid clientOrderId, CurrencyPair currencyPair)
		{
			var connection = new ApiConnection();
			var request = new RestRequest("history/order", Method.GET);
			request.Configure();

			request.AddParameter("clientOrderId", clientOrderId.ToString("N"));

			var exchangeConnectionSettings = _configurationService.GetExchangeConnectionSettings();

			var response = await connection.DoRequest(
				request,
				exchangeConnectionSettings.ApiKey,
				exchangeConnectionSettings.SecretKey);

			var order = response
				.ExtractData<Order[]>()
				.Select(entity => entity.ToOuterModel(currencyPair))
				.FirstOrDefault();
			return order;
		}

		public async Task<Infrastructure.Common.Models.Trading.Order> CreateOrder(Infrastructure.Common.Models.Trading.Order initialOrder, bool usePostOnly = false)
		{
			var innerModel = initialOrder.ToInnerModel();

			var connection = new ApiConnection();
			var request = new RestRequest("order", Method.POST);
			request.Configure();

			request.AddJsonBody(new
			{
				clientOrderId = innerModel.ClientId,
				symbol = innerModel.CurrencyPairId,
				side = innerModel.OrderSide,
				type = innerModel.OrderType,
				timeInForce = innerModel.TimeInForce,
				quantity = innerModel.Quantity.ToString("#0.########################"),
				price = innerModel.Price.ToString("#0.########################"),
				stopPrice = innerModel.StopPrice.ToString("#0.########################"),
				postOnly = usePostOnly
			});

			var exchangeConnectionSettings = _configurationService.GetExchangeConnectionSettings();

			var response = await connection.DoRequest(
				request,
				exchangeConnectionSettings.ApiKey,
				exchangeConnectionSettings.SecretKey);

			var responseOrder = response
				.ExtractData<Order>()
				?.ToOuterModel(initialOrder.CurrencyPair);
			return responseOrder;
		}

		public async Task<Infrastructure.Common.Models.Trading.Order> CancelOrder(Infrastructure.Common.Models.Trading.Order initialOrder)
		{
			var innerModel = initialOrder.ToInnerModel();

			var connection = new ApiConnection();
			var request = new RestRequest(String.Format("order/{0}", innerModel.ClientId), Method.DELETE);
			request.Configure();

			var exchangeConnectionSettings = _configurationService.GetExchangeConnectionSettings();

			var response = await connection.DoRequest(
				request,
				exchangeConnectionSettings.ApiKey,
				exchangeConnectionSettings.SecretKey);

			var responseOrder = response
				.ExtractData<Order>()
				?.ToOuterModel(initialOrder.CurrencyPair);
			return responseOrder;
		}
	}
}
