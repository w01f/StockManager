using System;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Utilities.Configuration.Models;

namespace StockManager.Infrastructure.Business.Trading.Helpers
{
	static class OrderHelper
	{
		public static void CalculateBuyOrderAmount(this Order order, TradingBallance tradingBallance, TradingSettings settings)
		{
			order.Quantity = Math.Floor((tradingBallance.Available * settings.MaxOrderUsingBallancePart / order.Price) / order.CurrencyPair.QuantityIncrement) * order.CurrencyPair.QuantityIncrement;
		}

		public static void CalculateSellOrderAmount(this Order order, TradingBallance tradingBallance, TradingSettings settings)
		{
			order.Quantity = Math.Floor(tradingBallance.Available * settings.MaxOrderUsingBallancePart / order.CurrencyPair.QuantityIncrement) * order.CurrencyPair.QuantityIncrement;
		}
	}
}
