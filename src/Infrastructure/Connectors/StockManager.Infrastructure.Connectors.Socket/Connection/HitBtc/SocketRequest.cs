using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Connection.HitBtc
{
	public abstract class SocketRequest
	{
		[JsonProperty(PropertyName = "method")]
		public string RequestMethodName { get; set; }
	}

	class SocketRequest<TRequestParameters>: SocketRequest where TRequestParameters: class 
	{
		[JsonProperty(PropertyName = "params")]
		public TRequestParameters RequestParameters { get; set; }
	}
}
