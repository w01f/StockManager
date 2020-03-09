using System;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Business.Trading.Models.Trading.Positions
{
	public static class TradingPositionMap
	{
		public static TradingPosition ToTradingPosition(this Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order> source, CurrencyPair currencyPair)
		{
			var target = new TradingPosition();

			target.OpenPositionOrder = source.Item1.ToModel(currencyPair);
			target.ClosePositionOrder = source.Item2.ToModel(currencyPair);
			target.StopLossOrder = source.Item3.ToModel(currencyPair);

			return target;
		}

		public static TradingPosition ToTradingPosition(this Tuple<Order, Order, Order> source)
		{
			var target = new TradingPosition();

			target.OpenPositionOrder = source.Item1;
			target.ClosePositionOrder = source.Item2;
			target.StopLossOrder = source.Item3;

			return target;
		}
	}
}
