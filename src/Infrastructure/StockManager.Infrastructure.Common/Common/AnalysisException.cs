using System;

namespace StockManager.Infrastructure.Common.Common
{
	public class AnalysisException : ApplicationException
	{
		public AnalysisException(string message) : base(message) { }

		public AnalysisException(string message, Exception innerException) : base(message, innerException) { }
	}
}
