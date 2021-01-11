using System;
using Newtonsoft.Json.Linq;

namespace StockManager.Infrastructure.Connectors.Socket.Connection.HitBtc
{
	class SocketSubscriptionAction : SocketAction
	{
		private ISocketSubscriptionRequest SubscriptionRequest => (ISocketSubscriptionRequest)Request;

		public string ResponseMethod { get; private set; }
		public bool NeedUnsubscribe => !string.IsNullOrWhiteSpace(SubscriptionRequest.UnsubscribeMethodName);

		public SocketSubscriptionAction(ISocketSubscriptionRequest socketRequest) : base((SocketRequest)socketRequest)
		{ }

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
			ResponseMethod = responseObject.Value<string>("method");

			return string.Equals(SubscriptionRequest.SnapshotMethodName, ResponseMethod, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(SubscriptionRequest.NotificationMethodName, ResponseMethod, StringComparison.OrdinalIgnoreCase);
		}
	}
}
