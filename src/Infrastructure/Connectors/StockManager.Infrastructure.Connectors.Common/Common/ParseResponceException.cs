using System;
using Newtonsoft.Json;

namespace StockManager.Infrastructure.Connectors.Common.Common
{
	public class ParseResponceException: Exception
	{
		public JsonException JsonException { get; }
		public string SourceData { get; }

		public ParseResponceException(JsonException jsonException, string sourceData)
		{
			JsonException = jsonException;
			SourceData = sourceData;
		}
	}
}
