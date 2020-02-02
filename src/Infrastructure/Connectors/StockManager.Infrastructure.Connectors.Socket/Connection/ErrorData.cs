using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	public class ErrorData
	{
		[JsonProperty(PropertyName = "code")]
		public int Code { get; set; }

		[JsonProperty(PropertyName = "message")]
		public string Message { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }
	}
}
