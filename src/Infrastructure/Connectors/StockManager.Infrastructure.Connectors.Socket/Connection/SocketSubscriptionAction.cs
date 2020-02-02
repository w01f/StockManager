using Newtonsoft.Json.Linq;

namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	class SocketSubscriptionAction : SocketAction
	{
		private ISocketSubscriptionRequest SubscriptionRequest => (ISocketSubscriptionRequest)SocketRequest;

		public SocketSubscriptionAction(ISocketSubscriptionRequest socketRequest) : base((SocketRequest)socketRequest)
		{
			ActionType = ActionType.Subscription;
		}

		protected override bool CanProcessResponse(string message)
		{
			var responseObject = JObject.Parse(message);
			if (!responseObject.ContainsKey("method")) return false;
			var methodName = responseObject.Value<string>("method");
			return SubscriptionRequest.NotificationMethodNames.Contains(methodName);
		}
	}
}
