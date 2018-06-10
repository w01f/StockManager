namespace StockManager.Infrastructure.Connectors.Common.Models
{
	public class CurrencyPair
	{
		public string Id { get; set; }
		public string BaseCurrencyId { get; set; }
		public string QuoteCurrencyId { get; set; }
		public decimal QuantityIncrement { get; set; }
		public decimal TickSize { get; set; }
		public decimal? TakeLiquidityRate { get; set; }
		public decimal? ProvideLiquidityRate { get; set; }
		public string FeeCurrencyId { get; set; }
	}
}
