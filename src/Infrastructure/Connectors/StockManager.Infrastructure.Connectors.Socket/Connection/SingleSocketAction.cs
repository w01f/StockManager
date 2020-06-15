using Newtonsoft.Json.Linq;

namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	class SingleSocketAction: SocketAction
	{
		private bool _completed;
		private ISingleSocketRequest SingleRequest => (ISingleSocketRequest)SocketRequest;

		public SingleSocketAction(ISingleSocketRequest socketRequest) : base((SocketRequest)socketRequest)
		{
			ActionType = ActionType.Request;
		}

		public void Complete()
		{
			_completed = true;
		}

		protected override bool CanProcessResponse(string message)
		{
			if (_completed)
				return false;

			if (!SingleRequest.NeedResponse)
				return false;

			var responseObject = JObject.Parse(message);
			if (!responseObject.ContainsKey("id")) return false;
			var id = responseObject.Value<int>("id");
			return SingleRequest.Id == id;
		}
	}
}
