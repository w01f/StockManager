using System;
using StockManager.Infrastructure.Business.Trading.Models.Trading.Settings;
using StockManager.Infrastructure.Common.Models.Trading;

namespace StockManager.Infrastructure.Business.Trading.Helpers
{
	static class OrderHelper
	{
		public static void CalculateOrderAmount(this Order order, TradingBallance tradingBallance, TradingSettings settings)
		{
			order.Quantity = Math.Floor((tradingBallance.Available * settings.MaxOrderUsingBallncePart / order.Price) / order.CurrencyPair.TickSize) * order.CurrencyPair.TickSize;
		}
	}
}
