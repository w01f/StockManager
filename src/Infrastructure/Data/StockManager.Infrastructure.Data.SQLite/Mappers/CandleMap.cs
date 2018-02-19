using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Core.Entities.Market;

namespace StockManager.Infrastructure.Data.SQLite.Mappers
{
	class CandleMap : BaseEntityMap<Candle>
	{
		public CandleMap(EntityTypeBuilder<Candle> entityBuilder) : base(entityBuilder)
		{
			entityBuilder.Property(target => target.CurrencyPair).IsRequired();
			entityBuilder.Property(target => target.Moment).IsRequired();
			entityBuilder.Property(target => target.Period).IsRequired();
			entityBuilder.Property(target => target.OpenPrice).IsRequired();
			entityBuilder.Property(target => target.ClosePrice).IsRequired();
			entityBuilder.Property(target => target.MaxPrice).IsRequired();
			entityBuilder.Property(target => target.MinPrice).IsRequired();
			entityBuilder.Property(target => target.VolumeInBaseCurrency).IsRequired();
			entityBuilder.Property(target => target.VolumeInQuoteCurrency).IsRequired();
			entityBuilder.HasIndex(target => new { target.CurrencyPair, target.Period, target.Moment }).IsUnique().HasName("UniqueCandle");
			entityBuilder.HasIndex(target => target.Moment);
		}
	}
}
