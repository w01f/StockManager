using System.Collections.Generic;
using StockManager.Infrastructure.Common.Models.Market;

namespace StockManager.Infrastructure.Business.Trading.EventArgs
{
	public class CurrencyPairsEventArgs
	{
		public IList<CurrencyPair> CurrencyPairs { get; }

		public CurrencyPairsEventArgs(IList<CurrencyPair> currencyPairs)
		{
			CurrencyPairs = currencyPairs;
		}
	}
}
