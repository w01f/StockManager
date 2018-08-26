using System;
using StockManager.Domain.Core.Common.Enums;

namespace StockManager.Domain.Core.Entities.Logging
{
	public class LogAction : BaseEntity
	{
		public virtual DateTime Moment { get; set; }
		public virtual LogActionType LogActionType { get; set; }
		public virtual string ExtendedOptionsEncoded { get; set; }
	}
}
