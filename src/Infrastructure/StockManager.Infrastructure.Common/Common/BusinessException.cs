using System;

namespace StockManager.Infrastructure.Common.Common
{
	public class BusinessException : ApplicationException
	{
		public string Details { get; set; }

		public BusinessException(string message) : base(message) { }

		public BusinessException(string message, Exception innerException) : base(message, innerException) { }
	}
}
