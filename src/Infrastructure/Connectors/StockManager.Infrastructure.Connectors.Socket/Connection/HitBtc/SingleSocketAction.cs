using System;
using Newtonsoft.Json.Linq;

namespace StockManager.Infrastructure.Connectors.Socket.Connection.HitBtc
{
	class SingleSocketAction : SocketAction
	{
		private ISingleSocketRequest SingleRequest => (ISingleSocketRequest)Request;

		public bool Completed { get; private set; }
		public event EventHandler<EventArgs> ErrorReceived;

		public SingleSocketAction(ISingleSocketRequest socketRequest) : base((SocketRequest)socketRequest)
		{ }

		public void Complete()
		{
			Completed = true;
		}

		protected override bool CanProcessResponse(string message)
		{
			if (Completed)
				return false;

			if (!SingleRequest.NeedResponse)
				return false;

			var responseObject = JObject.Parse(message);
			if (!responseObject.ContainsKey("id")) return false;
			var id = responseObject.Value<int?>("id");

			if (SingleRequest.Id != id)
				return false;

			if (message.Contains("error"))
				OnErrorReceived(EventArgs.Empty);

			return true;
		}

		private void OnErrorReceived(EventArgs e)
		{
			ErrorReceived?.Invoke(this, e);
		}
	}
}
