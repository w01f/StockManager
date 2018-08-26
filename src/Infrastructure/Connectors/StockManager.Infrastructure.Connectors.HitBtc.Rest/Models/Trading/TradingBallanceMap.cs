namespace StockManager.Infrastructure.Connectors.HitBtc.Rest.Models.Trading
{
	static class TradingBallanceMap
	{
		public static Infrastructure.Common.Models.Trading.TradingBallance ToOuterModel(this TradingBallance source)
		{
			var target = new Infrastructure.Common.Models.Trading.TradingBallance();

			target.CurrencyId = source.CurrencyId;
			target.Available = source.Available;
			target.Reserved = source.Reserved;

			return target;
		}
	}
}
