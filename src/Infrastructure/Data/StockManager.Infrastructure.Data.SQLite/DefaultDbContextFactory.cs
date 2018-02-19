using Microsoft.EntityFrameworkCore.Design;

namespace StockManager.Infrastructure.Data.SQLite
{
	public class DefaultDbContextFactory : IDesignTimeDbContextFactory<SQLiteDataContext>
	{
		public SQLiteDataContext CreateDbContext(string[] args)
		{
			return new SQLiteDataContext("Data Source=local_cache.db");
		}
	}
}
