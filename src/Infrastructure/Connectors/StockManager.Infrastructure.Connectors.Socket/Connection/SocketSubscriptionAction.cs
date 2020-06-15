using System;
using Newtonsoft.Json.Linq;

namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	class SocketSubscriptionAction : SocketAction
	{
		private ISocketSubscriptionRequest SubscriptionRequest => (ISocketSubscriptionRequest)SocketRequest;

		public bool NeedUnsubscribe => !string.IsNullOrWhiteSpace(SubscriptionRequest.UnsubscribeMethodName);

		public SocketSubscriptionAction(ISocketSubscriptionRequest socketRequest) : base((SocketRequest)socketRequest)
		{
			ActionType = ActionType.Subscription;
		}

		public ISingleSocketRequest GetUnsubscribeRequest()
		{
			var unsubscribeRequest = new SingleSocketRequest<object>
			{
				RequestMethodName = SubscriptionRequest.UnsubscribeMethodName,
				NeedResponse = false,
				RequestParameters = SubscriptionRequest.GetSubscriptionParameters(),
			};

			return unsubscribeRequest;
		}

		protected override bool CanProcessResponse(string message)
		{
			var responseObject = JObject.Parse(message);
			if (!responseObject.ContainsKey("method")) return false;
			var methodName = responseObject.Value<string>("method");

			return string.Equals(SubscriptionRequest.SnapshotMethodName, methodName, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(SubscriptionRequest.NotificationMethodName, methodName, StringComparison.OrdinalIgnoreCase);
		}
	}
}
