namespace StockManager.Infrastructure.Connectors.Rest.Connection
{
	class HitBtcConnection: ApiConnection
	{
		public HitBtcConnection() : base("https://api.hitbtc.com/api/2") { }
	}
}
