using System;
using StockManager.Domain.Core.Common.Enums;

namespace StockManager.Infrastructure.Utilities.Logging.Models
{
	public abstract class BaseLogAction
	{
		public DateTime Moment { get; set; }
		public abstract LogActionType LogActionType { get; }

		protected BaseLogAction()
		{
			Moment = DateTime.UtcNow;
		}
	}
}
