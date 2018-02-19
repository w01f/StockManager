using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.HitBtc.Rest.Connection
{
	class ApiError
	{
		[JsonProperty(PropertyName = "error")]
		public ErrorData Data { get; set; }
	}

	class ErrorData
	{
		[JsonProperty(PropertyName = "code")]
		public int Code { get; set; }

		[JsonProperty(PropertyName = "message")]
		public string Message { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }
	}
}
