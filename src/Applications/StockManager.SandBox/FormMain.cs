using System;
using System.Linq;
using System.Windows.Forms;
using Ninject;
using StockManager.Domain.Core.Enums;
using StockManager.Infrastructure.Connectors.Common.Services;

// ReSharper disable UnusedVariable
namespace StockManager.SandBox
{
	public partial class FormMain : Form
	{
		private readonly IMarketDataConnector _marketDataConnector;
		private readonly ITradingDataConnector _tradingDataConnector;

		public FormMain()
		{
			InitializeComponent();
		}

		[Inject]
		public FormMain(
			IMarketDataConnector marketDataConnector,
			ITradingDataConnector tradingDataConnector) : this()
		{
			_marketDataConnector = marketDataConnector;
			_tradingDataConnector = tradingDataConnector;
		}

		private async void OnRunTestClick(object sender, EventArgs e)
		{
			var currencyPair = (await _marketDataConnector.GetCurrensyPairs()).FirstOrDefault(item => item.Id == "DASHBTC");

			var orderId = Guid.NewGuid();

			var initialOrder = new Infrastructure.Common.Models.Trading.Order
			{
				CurrencyPair = currencyPair,
				ClientId = orderId,
				Role = OrderRoleType.ClosePosition,
				OrderSide = OrderSide.Sell,
				OrderType = OrderType.Limit,
				OrderStateType = OrderStateType.Suspended,
				TimeInForce = OrderTimeInForceType.GoodTillCancelled,
				Quantity = 0.001m,
				Price = 0.022m,
				StopPrice = 0
			};

			var newOrder = await _tradingDataConnector.CreateOrder(initialOrder, true);

			var cancelledOrder = await _tradingDataConnector.CancelOrder(newOrder);

			var testOrder = await _tradingDataConnector.GetOrderFromHistory(initialOrder.ClientId, currencyPair);

			MessageBox.Show(@"Passed");
		}
	}
}
