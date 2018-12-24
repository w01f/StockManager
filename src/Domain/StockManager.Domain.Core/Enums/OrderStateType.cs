namespace StockManager.Domain.Core.Enums
{
	public enum OrderStateType
	{
		Pending,
		Suspended,
		New,
		PartiallyFilled,
		Filled,
		Cancelled,
		Expired
	}
}
