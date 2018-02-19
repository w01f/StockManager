using System;
using Microsoft.EntityFrameworkCore;
using StockManager.Domain.Core.Entities.Market;
using StockManager.Infrastructure.Data.SQLite.Mappers;

namespace StockManager.Infrastructure.Data.SQLite
{
	public class SQLiteDataContext : DbContext
	{
		private readonly String _connectionString;

		public DbSet<Candle> Candles { get; set; }

		public SQLiteDataContext(string connectionString)
		{
			_connectionString = connectionString;
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite(_connectionString);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			new CandleMap(modelBuilder.Entity<Candle>());
		}
	}
}
