using System;
using StockManager.Infrastructure.Common.Models.Trading;
using StockManager.Infrastructure.Utilities.Configuration.Models;

namespace StockManager.Infrastructure.Business.Trading.Helpers
{
	static class OrderHelper
	{
		public static void CalculateBuyOrderQuantity(this Order order, TradingBallance tradingBallance, TradingSettings settings)
		{
			order.Quantity = Math.Floor((tradingBallance.Available * (1 - order.CurrencyPair.TakeLiquidityRate) * settings.MaxOrderUsingBallancePart / order.Price) / order.CurrencyPair.QuantityIncrement) * order.CurrencyPair.QuantityIncrement;
		}

		public static void CalculateSellOrderQuantity(this Order order, TradingBallance tradingBallance, TradingSettings settings)
		{
			order.Quantity = Math.Floor(tradingBallance.Available * (1 - order.CurrencyPair.TakeLiquidityRate) / order.CurrencyPair.QuantityIncrement) * order.CurrencyPair.QuantityIncrement;
		}
	}
}
