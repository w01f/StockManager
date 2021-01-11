namespace StockManager.Infrastructure.Connectors.Socket.Connection.HitBtc
{
	interface ISocketSubscriptionRequest
	{
		string SnapshotMethodName { get; }
		string NotificationMethodName { get; }
		string UnsubscribeMethodName { get; }

		object GetSubscriptionParameters();
	}

	class SocketSubscriptionRequest<TRequestParameters> : SocketRequest<TRequestParameters>, ISocketSubscriptionRequest where TRequestParameters : class
	{
		public string SnapshotMethodName { get; set; }
		public string NotificationMethodName { get; set; }
		public string UnsubscribeMethodName { get; set; }
		
		public object GetSubscriptionParameters()
		{
			return RequestParameters;
		}
	}
}
