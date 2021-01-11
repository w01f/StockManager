using System;

namespace StockManager.Infrastructure.Connectors.Socket.Connection.HitBtc
{
	abstract class SocketAction
	{
		public SocketRequest Request { get; }
		public Guid Id { get; }
		public event EventHandler<MessageReceivedEventArgs> ResponseReceived;

		protected SocketAction(SocketRequest socketRequest)
		{
			Id = Guid.NewGuid();
			Request = socketRequest;
		}

		public string GetMessage()
		{
			return Request.EncodeSocketRequest();
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
