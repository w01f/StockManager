namespace StockManager.Domain.Core.Enums
{
	public enum OrderStateType
	{
		Pending = 0,

		//Stop orders states
		Suspended = 1,

		//Limit order states
		New = 2,
		PartiallyFilled = 3,
		Expired = 6,

		//Common order states
		Filled = 4,
		Cancelled = 5,
	}
}
