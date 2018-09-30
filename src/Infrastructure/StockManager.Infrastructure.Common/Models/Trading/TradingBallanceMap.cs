namespace StockManager.Infrastructure.Common.Models.Trading
{
	public static class TradingBallanceMap
	{
		public static TradingBallance ToModel(this Domain.Core.Entities.Trading.TradingBallance source)
		{
			var target = new TradingBallance();

			target.CurrencyId = source.CurrencyId;
			target.Available = source.Available;
			target.Reserved = source.Reserved;

			return target;
		}

		public static Domain.Core.Entities.Trading.TradingBallance ToEntity(this TradingBallance source, Domain.Core.Entities.Trading.TradingBallance target = null)
		{
			if (target == null)
				target = new Domain.Core.Entities.Trading.TradingBallance();

			target.CurrencyId = source.CurrencyId;
			target.Available = source.Available;
			target.Reserved = source.Reserved;

			return target;
		}
	}
}
