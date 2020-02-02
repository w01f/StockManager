namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	class HitBtcConnection : ApiConnection
	{
		public HitBtcConnection() : base("wss://api.hitbtc.com/api/2/ws") { }
	}
}
