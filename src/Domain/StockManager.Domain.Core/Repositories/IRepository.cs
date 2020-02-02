using System.Collections.Generic;
using System.Linq;
using StockManager.Domain.Core.Entities;

namespace StockManager.Domain.Core.Repositories
{
	public interface IRepository<TEntity> where TEntity : BaseEntity
	{
		IQueryable<TEntity> GetAll();
		TEntity Get(long id);
		void Insert(TEntity entity);
		void Insert(IEnumerable<TEntity> entities);
		void Update(TEntity entity);
		void Delete(TEntity entity);
		void SaveChanges();
	}
}
