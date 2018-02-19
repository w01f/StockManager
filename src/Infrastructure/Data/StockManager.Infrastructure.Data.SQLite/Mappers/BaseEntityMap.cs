using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Core.Entities;

namespace StockManager.Infrastructure.Data.SQLite.Mappers
{
	abstract class BaseEntityMap<TEntity> where TEntity : BaseEntity
	{
		protected BaseEntityMap(EntityTypeBuilder<TEntity> entityBuilder)
		{
			entityBuilder.HasKey(t => t.Id);
		}
	}
}
