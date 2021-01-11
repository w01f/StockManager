using System;
using Newtonsoft.Json;
using StockManager.Infrastructure.Connectors.Common.Common;

namespace StockManager.Infrastructure.Connectors.Socket.Connection.HitBtc
{
	static class ApiExtensions
	{
		public static string EncodeSocketRequest(this SocketRequest socketRequest)
		{
			var serializerSettings = new RestSerializeSettings();
			serializerSettings.TypeNameHandling = TypeNameHandling.None;
			serializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
			return JsonConvert.SerializeObject(socketRequest, serializerSettings);
		}

		public static SocketResponse<TResponseResult> DecodeSocketResponse<TResponseResult>(string responseMessage) where TResponseResult : class
		{
			try
			{
				var serializerSettings = new RestSerializeSettings();
				return !String.IsNullOrEmpty(responseMessage) ?
					JsonConvert.DeserializeObject<SocketResponse<TResponseResult>>(responseMessage, serializerSettings) :
					null;
			}
			catch (JsonException e)
			{
				throw new ParseResponseException(e, responseMessage);
			}
		}

		public static SocketNotification<TNotificationParameters> DecodeSocketNotification<TNotificationParameters>(string responseMessage) where TNotificationParameters : class
		{
			try
			{
				var serializerSettings = new RestSerializeSettings();
				return !String.IsNullOrEmpty(responseMessage) ?
					JsonConvert.DeserializeObject<SocketNotification<TNotificationParameters>>(responseMessage, serializerSettings) :
					null;
			}
			catch (JsonException e)
			{
				throw new ParseResponseException(e, responseMessage);
			}
		}
	}
}
