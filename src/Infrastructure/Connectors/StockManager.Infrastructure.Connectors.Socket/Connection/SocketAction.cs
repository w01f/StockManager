using System;

namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	abstract class SocketAction
	{
		protected readonly SocketRequest SocketRequest;

		public ActionType ActionType { get; protected set; }
		public event EventHandler<MessageReceivedEventArgs> ResponseReceived;

		protected SocketAction(SocketRequest socketRequest)
		{
			SocketRequest = socketRequest;
		}

		public string GetMessage()
		{
			return SocketRequest.EncodeSocketRequest();
		}

		public bool ProcessResponse(string message)
		{
			if (!CanProcessResponse(message)) 
				return false;
			
			OnMessageReceived(message);
			return true;
		}

		protected abstract bool CanProcessResponse(string message);

		private void OnMessageReceived(string message)
		{
			ResponseReceived?.Invoke(this, new MessageReceivedEventArgs(message));
		}
	}
}
