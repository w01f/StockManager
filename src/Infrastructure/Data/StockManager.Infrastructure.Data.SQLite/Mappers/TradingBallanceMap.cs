using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Core.Entities.Trading;

namespace StockManager.Infrastructure.Data.SQLite.Mappers
{
	class TradingBallanceMap : BaseEntityMap<TradingBallance>
	{
		public TradingBallanceMap(EntityTypeBuilder<TradingBallance> entityBuilder) : base(entityBuilder)
		{
			entityBuilder.Property(target => target.CurrencyId).IsRequired();
			entityBuilder.HasIndex(target => new
			{
				target.CurrencyId
			}).IsUnique().HasName("TradingBallanceCurrency");
		}
	}
}
