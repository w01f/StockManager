using System;

namespace StockManager.Infrastructure.Connectors.Common.Common
{
	public class OrderBookUpdatedEventArgs: EventArgs
	{
		public string CurrencyPairId { get; }

		public OrderBookUpdatedEventArgs(string currencyPairId)
		{
			CurrencyPairId = currencyPairId;
		}
	}
}
