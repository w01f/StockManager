using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.RequestParameters
{
	class LoginRequestParameters
	{
		[JsonProperty(PropertyName = "algo")]
		public string Algorithm { get; set; }

		[JsonProperty(PropertyName = "pKey")]
		public string ApiKey { get; set; }

		[JsonProperty(PropertyName = "sKey")]
		public string SecretKey { get; set; }
	}
}
