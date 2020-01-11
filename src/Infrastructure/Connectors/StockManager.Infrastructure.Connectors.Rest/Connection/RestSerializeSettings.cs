using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace StockManager.Infrastructure.Connectors.Rest.Connection
{
	class RestSerializeSettings : JsonSerializerSettings
	{
		public RestSerializeSettings()
		{
			ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
			PreserveReferencesHandling = PreserveReferencesHandling.None;
			TypeNameHandling = TypeNameHandling.All;
			Formatting = Formatting.None;

			ContractResolver = new CamelCasePropertyNamesContractResolver();
		}
	}
}
