using System;

namespace StockManager.Infrastructure.Connectors.Socket.Connection.HitBtc
{
	public class RequestIdGenerator
	{
		private readonly Random _generator = new Random();

		public int CreateId()
		{
			return _generator.Next();
		}
	}
}
