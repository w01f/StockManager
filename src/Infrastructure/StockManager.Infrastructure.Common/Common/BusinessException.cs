using System;

namespace StockManager.Infrastructure.Common.Common
{
	public class BusinessException : ApplicationException
	{
		public BusinessException(string message) : base(message) { }

		public BusinessException(string message, Exception innerException) : base(message, innerException) { }
	}
}
