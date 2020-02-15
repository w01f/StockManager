using System;
using StockManager.Domain.Core.Enums;

namespace StockManager.Infrastructure.Connectors.Common.Common
{
	public class CandlesUpdatedEventArgs: EventArgs
	{
		public string CurrencyPairId { get; }
		public CandlePeriod Period { get; }

		public CandlesUpdatedEventArgs(string currencyPairId, CandlePeriod period)
		{
			CurrencyPairId = currencyPairId;
			Period = period;
		}
	}
}
