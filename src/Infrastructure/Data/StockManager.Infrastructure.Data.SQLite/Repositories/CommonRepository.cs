using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StockManager.Domain.Core.Entities;
using StockManager.Domain.Core.Repositories;

namespace StockManager.Infrastructure.Data.SQLite.Repositories
{
	public class CommonRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
	{
		private readonly SQLiteDataContext _context;
		private readonly DbSet<TEntity> _entities;

		public CommonRepository(SQLiteDataContext context)
		{
			_context = context;
			_entities = context.Set<TEntity>();
		}

		public virtual IEnumerable<TEntity> GetAll()
		{
			return _entities.AsEnumerable();
		}

		public virtual TEntity Get(Int64 id)
		{
			return _entities.SingleOrDefault(s => s.Id == id);
		}

		public virtual void Insert(TEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));
			_entities.Add(entity);
			SaveChanges();
		}

		public virtual void Insert(IEnumerable<TEntity> entities)
		{
			if (entities == null)
				throw new ArgumentNullException(nameof(entities));
			foreach (var entity in entities)
				_entities.Add(entity);
			SaveChanges();
		}

		public virtual void Update(TEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));
			SaveChanges();
		}

		public virtual void Delete(TEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));
			_entities.Remove(entity);
			SaveChanges();
		}

		public virtual void SaveChanges()
		{
			_context.SaveChanges();
		}
	}
}
