using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	public class SocketNotification<TNotificationParameters> where TNotificationParameters : class
	{
		[JsonProperty(PropertyName = "method")]
		public string MethodName { get; set; }

		[JsonProperty(PropertyName = "params")]
		public TNotificationParameters NotificationParameters { get; set; }

		[JsonProperty(PropertyName = "error")]
		public ErrorData ErrorData { get; set; }
	}
}
