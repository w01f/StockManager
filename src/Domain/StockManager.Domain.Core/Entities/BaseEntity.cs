using System;

namespace StockManager.Domain.Core.Entities
{
	public abstract class BaseEntity
	{
		public virtual Int64 Id { get; set; }
	}
}
