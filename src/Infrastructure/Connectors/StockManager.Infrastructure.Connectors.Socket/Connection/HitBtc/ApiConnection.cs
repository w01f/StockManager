using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using StockManager.Infrastructure.Connectors.Common.Common;
using StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.NotificationParameters;
using StockManager.Infrastructure.Connectors.Socket.Models.HitBtc.RequestParameters;
using StockManager.Infrastructure.Utilities.Configuration.Models;
using Websocket.Client;

namespace StockManager.Infrastructure.Connectors.Socket.Connection.HitBtc
{
	class ApiConnection
	{
		private readonly string _baseUrl;
		private readonly ExchangeConnectionSettings _connectionSettings;
		private WebsocketClient _socketConnection;
		private bool _restoreConnection = true;
		private readonly ConcurrentDictionary<Guid, SocketAction> _socketActions = new ConcurrentDictionary<Guid, SocketAction>();
		private readonly RequestIdGenerator _idGenerator = new RequestIdGenerator();

		public event EventHandler<UnhandledExceptionEventArgs> Error;

		public ApiConnection(ExchangeConnectionSettings connectionSettings)
		{
			_baseUrl = "wss://api.hitbtc.com/api/2/ws";
			_connectionSettings = connectionSettings;
		}

		public async Task<TResponseResult> DoRequest<TResponseResult>(ISingleSocketRequest socketRequest, Action errorCallback = null) where TResponseResult : class
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
			socketAction.ErrorReceived += (o, e) =>
			{
				errorCallback?.Invoke();
			};

			RunAction(socketAction);

			if (!socketRequest.NeedResponse)
				return Activator.CreateInstance<TResponseResult>();

			while (!responseReceived)
			{
				await Task.Delay(50);
			}

			if (socketResponse?.ErrorData != null)
				throw new ConnectorException($"{socketResponse?.ErrorData.Message}. {socketResponse?.ErrorData.Description}");

			return socketResponse?.ResponseData;
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

		public async Task Connect()
		{
			if (_socketConnection != null && _socketConnection.IsRunning)
				return;

			var uri = new Uri(_baseUrl);
			_socketConnection = new WebsocketClient(uri);
			_socketConnection.ReconnectTimeout = TimeSpan.FromSeconds(60);
			_socketConnection.ErrorReconnectTimeout = TimeSpan.FromSeconds(30);
			_socketConnection.ReconnectionHappened.Subscribe(info =>
			{
				if (!_restoreConnection)
					return;
				Login().Wait();
				RestoreSubscriptions();
			});
			_socketConnection.DisconnectionHappened.Subscribe(info =>
			{
				info.CancelReconnection = !_restoreConnection;
				if (info.Type == DisconnectionType.ByUser ||
					info.Type == DisconnectionType.Exit ||
					info.Type == DisconnectionType.NoMessageReceived)
					return;
				OnError(new UnhandledExceptionEventArgs(info.Exception, false));
			});
			_socketConnection.MessageReceived.Subscribe(msg =>
			{
				switch (msg.MessageType)
				{
					case WebSocketMessageType.Text:
						try
						{
							foreach (var socketAction in _socketActions.Values.ToList())
							{
								var result = socketAction.ProcessResponse(msg.Text);

								if (socketAction is SingleSocketAction singleSocketAction)
								{
									if(result)
										singleSocketAction.Complete();

									if (singleSocketAction.Completed)
									{
										_socketActions.TryRemove(singleSocketAction.Id, out _);
										break;
									}
								}
							}
						}
						catch (Exception exception)
						{
							OnError(new UnhandledExceptionEventArgs(exception, false));
						}
						break;
				}
			});

			await _socketConnection.Start();
		}

		public async Task Disconnect()
		{
			_restoreConnection = false;

			if (_socketConnection.IsRunning)
				await CloseSubscriptions();

			await _socketConnection.Stop(WebSocketCloseStatus.NormalClosure, String.Empty);

			_socketConnection = null;

			Error = null;
		}

		private void RunAction(SocketAction action)
		{
			_socketActions.TryAdd(action.Id, action);
			_socketConnection.Send(action.GetMessage());
		}

		private async Task Login()
		{
			if (!string.IsNullOrWhiteSpace(_connectionSettings.ApiKey) && !string.IsNullOrWhiteSpace(_connectionSettings.SecretKey))
			{
				var loginRequest = new SingleSocketRequest<LoginRequestParameters>
				{
					RequestMethodName = "login",
					NeedResponse = false,
					RequestParameters = new LoginRequestParameters
					{
						Algorithm = "BASIC",
						ApiKey = _connectionSettings.ApiKey,
						SecretKey = _connectionSettings.SecretKey
					}
				};

				await DoRequest<EmptyResponse>(loginRequest);
			}
		}

		private void RestoreSubscriptions()
		{
			foreach (var socketSubscriptionAction in _socketActions.OfType<SocketSubscriptionAction>().ToList())
				_socketConnection.Send(socketSubscriptionAction.GetMessage());
		}

		private async Task CloseSubscriptions()
		{
			foreach (var socketSubscriptionAction in _socketActions.OfType<SocketSubscriptionAction>().Where(action => action.NeedUnsubscribe).ToList())
				await DoRequest<EmptyResponse>(socketSubscriptionAction.GetUnsubscribeRequest());
		}

		private void OnError(UnhandledExceptionEventArgs e)
		{
			Error?.Invoke(this, e);
		}
	}
}
