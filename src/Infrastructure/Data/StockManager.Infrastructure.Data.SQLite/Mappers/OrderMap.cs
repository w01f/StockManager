using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Core.Entities.Trading;

namespace StockManager.Infrastructure.Data.SQLite.Mappers
{
	class OrderMap : BaseEntityMap<Order>
	{
		public OrderMap(EntityTypeBuilder<Order> entityBuilder) : base(entityBuilder)
		{
			entityBuilder.Property(target => target.ExtId).IsRequired();
			entityBuilder.Property(target => target.ClientId).IsRequired();
			entityBuilder.Property(target => target.CurrencyPair).IsRequired();
			entityBuilder.Property(target => target.OrderSide).IsRequired();
			entityBuilder.Property(target => target.OrderType).IsRequired();
			entityBuilder.Property(target => target.OrderStateType).IsRequired();
			entityBuilder.Property(target => target.Quantity).IsRequired();
			entityBuilder.Property(target => target.Price).IsRequired();
			entityBuilder.Property(target => target.Created).IsRequired();
			entityBuilder.HasIndex(target => new
			{
				target.ExtId
			}).IsUnique().HasName("ByExtId");
			entityBuilder.HasIndex(target => new
			{
				target.ClientId
			}).IsUnique().HasName("ByClientId");
			entityBuilder.HasIndex(target => new
			{
				target.CurrencyPair,
			}).HasName("CurrencyPair");
		}
	}
}
