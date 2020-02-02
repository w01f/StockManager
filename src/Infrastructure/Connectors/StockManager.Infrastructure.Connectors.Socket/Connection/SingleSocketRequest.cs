using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	interface ISingleSocketRequest
	{
		int Id { get; set; }
	}

	class SingleSocketRequest<TRequestParameters>: SocketRequest<TRequestParameters>, ISingleSocketRequest where TRequestParameters: class 
	{
		[JsonProperty(PropertyName = "id")]
		public int Id { get; set; }
	}
}
