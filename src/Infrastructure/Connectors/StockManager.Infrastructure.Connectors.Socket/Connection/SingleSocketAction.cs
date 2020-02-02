using Newtonsoft.Json.Linq;

namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	class SingleSocketAction: SocketAction
	{
		private ISingleSocketRequest SingleRequest => (ISingleSocketRequest)SocketRequest;

		public SingleSocketAction(ISingleSocketRequest socketRequest) : base((SocketRequest)socketRequest)
		{
			ActionType = ActionType.Request;
		}

		protected override bool CanProcessResponse(string message)
		{
			var responseObject = JObject.Parse(message);
			if (!responseObject.ContainsKey("id")) return false;
			var id = responseObject.Value<int>("id");
			return SingleRequest.Id == id;
		}
	}
}
