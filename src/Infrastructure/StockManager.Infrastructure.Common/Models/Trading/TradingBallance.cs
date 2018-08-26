namespace StockManager.Infrastructure.Common.Models.Trading
{
	public class TradingBallance
	{
		public string CurrencyId { get; set; }
		public decimal Available { get; set; }
		public decimal Reserved { get; set; }
	}
}
