using System;

namespace StockManager.Infrastructure.Connectors.Common.Common
{
	public class ConnectorException : ApplicationException
	{
		public ConnectorException(string message) : base(message) { }

		public ConnectorException(string message, Exception innerException) : base(message, innerException) { }
	}
}
