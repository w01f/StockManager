﻿using System.Collections.Generic;
using System.Threading.Tasks;
using StockManager.Domain.Core.Common.Enums;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Connectors.Common.Models;

namespace StockManager.Infrastructure.Connectors.Common.Services
{
	public interface IMarketDataConnector
	{
		Task<IList<CurrencyPair>> GetCurrensyPairs();
		Task<IList<Candle>> GetCandles(string currencyPairId, CandlePeriod period, int limit);
	}
}
