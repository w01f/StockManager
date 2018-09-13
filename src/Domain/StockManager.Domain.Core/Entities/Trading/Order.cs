using System;
using StockManager.Domain.Core.Enums;

namespace StockManager.Domain.Core.Entities.Trading
{
	public class Order: BaseEntity
	{
		public virtual Int64 ExtId { get; set; }
		public virtual Guid ClientId { get; set; }
		public virtual Guid ParentClientId { get; set; }
		public virtual string CurrencyPair { get; set; }
		public virtual OrderSide OrderSide { get; set; }
		public virtual OrderType OrderType { get; set; }
		public virtual OrderStateType OrderStateType { get; set; }
		public virtual decimal Quantity { get; set; }
		public virtual decimal Price { get; set; }
		public virtual decimal? StopPrice { get; set; }
		public virtual DateTime Created { get; set; }
		public virtual DateTime Updated { get; set; }
	}
}
