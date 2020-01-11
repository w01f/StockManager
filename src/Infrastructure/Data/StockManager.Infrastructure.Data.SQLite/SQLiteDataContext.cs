using Microsoft.EntityFrameworkCore;
using StockManager.Domain.Core.Entities.Logging;
using StockManager.Domain.Core.Entities.Market;
using StockManager.Domain.Core.Entities.Trading;
using StockManager.Infrastructure.Data.SQLite.Mappers;
using StockManager.Infrastructure.Utilities.Configuration.Services;

// ReSharper disable ObjectCreationAsStatement
namespace StockManager.Infrastructure.Data.SQLite
{
	public class SQLiteDataContext : DbContext
	{
		private readonly string _connectionString;

		public DbSet<Candle> Candles { get; set; }
		public DbSet<Order> Orders { get; set; }
		public DbSet<TradingBallance> TradingBallance { get; set; }
		public DbSet<OrderHistory> OrderHistory { get; set; }
		public DbSet<LogAction> LogActions { get; set; }

		public SQLiteDataContext(string connectionString)
		{
			_connectionString = connectionString;
		}

		public SQLiteDataContext(ConfigurationService configurationService)
			: this(configurationService.GetDatabaseConnectionSettings().ConnectionString) { }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite(_connectionString);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			new CandleMap(modelBuilder.Entity<Candle>());
			new OrderMap(modelBuilder.Entity<Order>());
			new TradingBallanceMap(modelBuilder.Entity<TradingBallance>());
			new OrderHistoryMap(modelBuilder.Entity<OrderHistory>());
			new LogActionMap(modelBuilder.Entity<LogAction>());
		}
	}
}
