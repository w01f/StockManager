using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Core.Entities.Logging;

namespace StockManager.Infrastructure.Data.SQLite.Mappers
{
	class LogActionMap : BaseEntityMap<LogAction>
	{
		public LogActionMap(EntityTypeBuilder<LogAction> entityBuilder) : base(entityBuilder)
		{
			entityBuilder.Property(target => target.Moment).IsRequired();
			entityBuilder.Property(target => target.LogActionType).IsRequired();
			entityBuilder.Property(target => target.ExtendedOptionsEncoded).HasColumnType("text");
			entityBuilder.HasIndex(target => new { target.Moment, ActionType = target.LogActionType }).IsUnique().HasName("UniqueLogAction");
			entityBuilder.HasIndex(target => target.Moment);
		}
	}
}
