using System;

namespace StockManager.Infrastructure.Common.Common
{
	public class BusinessWarning : ApplicationException
	{
		public string Details { get; set; }

		public BusinessWarning(string message) : base(message) { }

		public BusinessWarning(string message, Exception innerException) : base(message, innerException) { }
	}
}
