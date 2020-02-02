using System;
using Newtonsoft.Json;
using RestSharp;
using StockManager.Infrastructure.Connectors.Common.Common;

namespace StockManager.Infrastructure.Connectors.Rest.Connection
{
	static class ApiExtensions
	{
		public static void ConfigureRequest(this RestRequest target)
		{
			target.AddHeader("Content-Type", "application/json");
			target.AddHeader("Accept", "*/*");
			target.AddHeader("Connection", "keep-alive");
			target.RequestFormat = DataFormat.Json;
		}

		public static TData ExtractResponseData<TData>(this IRestResponse target)
		{
			try
			{
				var serializerSettings = new RestSerializeSettings();
				return !String.IsNullOrEmpty(target.Content) ?
					JsonConvert.DeserializeObject<TData>(target.Content, serializerSettings) :
					default(TData);
			}
			catch (JsonException e)
			{
				throw new ParseResponseException(e, target?.Content);
			}
		}
	}
}
