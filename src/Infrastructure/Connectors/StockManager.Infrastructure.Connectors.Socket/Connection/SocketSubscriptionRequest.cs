using System.Collections.Generic;

namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	interface ISocketSubscriptionRequest
	{
		IList<string> NotificationMethodNames { get; }
	}

	class SocketSubscriptionRequest<TRequestParameters> : SocketRequest<TRequestParameters>, ISocketSubscriptionRequest where TRequestParameters : class
	{
		public IList<string> NotificationMethodNames { get; } = new List<string>();
	}
}
