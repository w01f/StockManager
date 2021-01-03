using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	class ApiError
	{
		[JsonProperty(PropertyName = "error")]
		public ErrorData Data { get; set; }
	}
}
