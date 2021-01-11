using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Connection.HitBtc
{
	interface ISingleSocketRequest
	{
		int Id { get; set; }
		bool NeedResponse { get; }
	} 

	class SingleSocketRequest<TRequestParameters>: SocketRequest<TRequestParameters>, ISingleSocketRequest where TRequestParameters: class 
	{
		[JsonProperty(PropertyName = "id")]
		public int Id { get; set; }
		public bool NeedResponse { get; set; }

		public SingleSocketRequest()
		{
			NeedResponse = true;
		}
	}
}
