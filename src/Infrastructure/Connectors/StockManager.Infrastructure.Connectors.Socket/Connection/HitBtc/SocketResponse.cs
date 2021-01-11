using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Connection.HitBtc
{
	public class SocketResponse<TResponseData> where TResponseData : class
	{
		[JsonProperty(PropertyName = "id")]
		public int Id { get; set; }

		[JsonProperty(PropertyName = "result")]
		public TResponseData ResponseData { get; set; }

		[JsonProperty(PropertyName = "error")]
		public ErrorData ErrorData { get; set; }
	}
}
