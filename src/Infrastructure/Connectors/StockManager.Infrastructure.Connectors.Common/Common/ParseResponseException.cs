using System;
using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Common.Common
{
	public class ParseResponseException: Exception
	{
		public JsonException JsonException { get; }
		public string SourceData { get; }

		public ParseResponseException(JsonException jsonException, string sourceData)
		{
			JsonException = jsonException;
			SourceData = sourceData;
		}
	}
}
