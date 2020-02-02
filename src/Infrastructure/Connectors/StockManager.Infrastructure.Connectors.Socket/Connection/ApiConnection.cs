using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.Infrastructure.Connectors.Common.Common;
using WebSocket4Net;

namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	abstract class ApiConnection
	{
		private readonly string _baseUrl;
		private WebSocket _socket;
		private readonly IList<SocketAction> _socketActions = new List<SocketAction>();
		private readonly RequestIdGenerator _idGenerator = new RequestIdGenerator();

		protected ApiConnection(string baseUrl)
		{
			_baseUrl = baseUrl;
		}

		public async Task<TResponseResult> DoRequest<TResponseResult>(ISingleSocketRequest socketRequest) where TResponseResult : class
		{
			await Connect();

			SocketResponse<TResponseResult> socketResponse = null;
			var responseReceived = false;

			socketRequest.Id = _idGenerator.CreateId();
			var socketAction = new SingleSocketAction(socketRequest);
			socketAction.ResponseReceived += (o, e) =>
			{
				socketResponse = ApiExtensions.DecodeSocketResponse<TResponseResult>(e.Message);
				responseReceived = true;
			};
			RunAction(socketAction);

			while (!responseReceived)
			{
				await Task.Delay(100);
			}

			if (socketResponse.ErrorData != null)
				throw new ConnectorException($"{socketResponse.ErrorData.Message}. {socketResponse.ErrorData.Description}");

			return socketResponse.ResponseData;
		}

		public async Task Subscribe<TNotificationData>(ISocketSubscriptionRequest socketRequest, Action<TNotificationData> callback) where TNotificationData : class
		{
			await Connect();
			
			var socketAction = new SocketSubscriptionAction(socketRequest);
			socketAction.ResponseReceived += (o, e) =>
			{
				var socketNotification = ApiExtensions.DecodeSocketNotification<TNotificationData>(e.Message);
				if (socketNotification.ErrorData != null)
					throw new ConnectorException($"{socketNotification.ErrorData.Message}. {socketNotification.ErrorData.Description}");
				callback(socketNotification.NotificationParameters);
			};
			RunAction(socketAction);
		}

		private void RunAction(SocketAction action)
		{
			_socketActions.Add(action);
			_socket.Send(action.GetMessage());
		}

		private async Task Connect()
		{
			if (_socket != null && _socket.State == WebSocketState.Open)
				return;

			_socket = new WebSocket(_baseUrl);
			_socket.Error += (o, e) => throw e.Exception;
			_socket.MessageReceived += (o, e) =>
			{
				foreach (var socketAction in _socketActions.ToList())
				{
					var result = socketAction.ProcessResponse(e.Message);

					if (!result)
						continue;

					if (socketAction.ActionType == ActionType.Request)
						_socketActions.Remove(socketAction);
					break;
				}
			};

			_socket.Open();

			while (_socket.State != WebSocketState.Open)
			{
				await Task.Delay(100);
			}
		}
	}
}
