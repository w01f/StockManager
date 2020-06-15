using StockManager.Infrastructure.Utilities.Configuration.Models;

namespace StockManager.Infrastructure.Connectors.Socket.Connection
{
	class HitBtcConnection : ApiConnection
	{
		public HitBtcConnection(ExchangeConnectionSettings connectionSettings) : base("wss://api.hitbtc.com/api/2/ws", connectionSettings) { }
	}
}
