namespace StockManager.Domain.Core.Enums
{
	public enum OrderTimeInForceType
	{
		GoodTillCancelled,
		ImmediateOrCancel,
		FillOrKill,
		UntillTheEndOfTheDay,
		GoodTillDate
	}
}
