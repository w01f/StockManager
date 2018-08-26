using System;
using StockManager.Infrastructure.Common.Models.Market;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Business.Trading.Models.Trading.Orders
{
	public static class OrderPairMap
	{
		public static OrderPair ToModel(this Tuple<Domain.Core.Entities.Trading.Order, Domain.Core.Entities.Trading.Order> source, CurrencyPair currencyPair)
		{
			var target = new OrderPair();

			target.InitialOrder = source.Item1.ToModel(currencyPair);
			target.StopLossOrder = source.Item2.ToModel(currencyPair);

			return target;
		}
	}
}
