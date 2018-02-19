using System.Collections.Generic;
using StockManager.Domain.Core.Entities;

namespace StockManager.Domain.Core.Repositories
{
	public interface IRepository<TEntity> where TEntity : BaseEntity
	{
		IEnumerable<TEntity> GetAll();
		TEntity Get(long id);
		void Insert(TEntity entity);
		void Insert(IEnumerable<TEntity> entities);
		void Update(TEntity entity);
		void Delete(TEntity entity);
		void Remove(TEntity entity);
		void SaveChanges();
	}
}
