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
		private readonly IStockRestConnector _stockRestConnector;

		public FormMain()
		{
			InitializeComponent();
		}

		[Inject]
		public FormMain(
			IStockRestConnector stockRestConnector) : this()
		{
			_stockRestConnector = stockRestConnector;
		}

		private async void OnRunTestClick(object sender, EventArgs e)
		{
			var currencyPair = (await _stockRestConnector.GetCurrencyPairs()).FirstOrDefault(item => item.Id == "DASHBTC");

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

			var newOrder = await _stockRestConnector.CreateOrder(initialOrder, true);

			var cancelledOrder = await _stockRestConnector.CancelOrder(newOrder);

			var testOrder = await _stockRestConnector.GetOrderFromHistory(initialOrder.ClientId, currencyPair);

			MessageBox.Show(@"Passed");
		}
	}
}
