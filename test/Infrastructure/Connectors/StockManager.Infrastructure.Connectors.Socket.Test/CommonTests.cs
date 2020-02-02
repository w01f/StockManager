using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StockManager.Infrastructure.Connectors.Socket.Connection;

namespace StockManager.Infrastructure.Connectors.Socket.Test
{
	[TestClass]
	public class CommonTests
	{
		[TestMethod]
		public void RequestIdsAreUnique()
		{
			var idGenerator = new RequestIdGenerator();
			var generatedIds = new List<int>();

			for (int i = 0; i < 5; i++)
			{
				var id = idGenerator.CreateId();
				Console.WriteLine(id);
				Assert.IsFalse(generatedIds.Contains(id));
				generatedIds.Add(id);
				Task.Delay(1000);
			}
		}
	}
}
