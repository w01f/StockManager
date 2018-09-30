namespace StockManager.Domain.Core.Entities.Trading
{
	public class TradingBallance : BaseEntity
	{
		public virtual string CurrencyId { get; set; }
		public virtual decimal Available { get; set; }
		public virtual decimal Reserved { get; set; }
	}
}
