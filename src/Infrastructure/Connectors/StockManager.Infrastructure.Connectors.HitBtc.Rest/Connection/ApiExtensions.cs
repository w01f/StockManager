using System;
using Newtonsoft.Json;
using RestSharp;

namespace StockManager.Infrastructure.Connectors.HitBtc.Rest.Connection
{
	static class ApiExtensions
	{
		public static void Configure(this RestRequest target)
		{
			target.AddHeader("Content-Type", "application/json");
			target.AddHeader("Accept", "*/*");
			target.RequestFormat = DataFormat.Json;
		}

		public static TData ExtractData<TData>(this IRestResponse target)
		{
			var serializerSettings = new RestSerializeSettings();
			return !String.IsNullOrEmpty(target.Content) ?
				JsonConvert.DeserializeObject<TData>(target.Content, serializerSettings) :
				default(TData);
		}
	}
}
