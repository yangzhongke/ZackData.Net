using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using ZackData.NetStandard.Exceptions;

namespace ZackData.NetStandard
{
    public class BaseEFCrudRepository<TEntity, ID> : ICrudRepository<TEntity, ID> where TEntity : class
    {
        public virtual bool AutoSave { get; set; } = true;
        protected Func<DbContext> dbContextCreator;

        public BaseEFCrudRepository(Func<DbContext> dbContextCreator)
        {
            this.dbContextCreator = dbContextCreator;
        }

        protected DbSet<TEntity> dbSet
        {
            get
            {
                return this.dbContextCreator().Set<TEntity>();
            }
        }

        private void TrySaveChanges()
        {
            if(AutoSave)
            {
                dbContextCreator().SaveChanges();
            }
        }

        public TEntity AddNew(TEntity entity)
        {
            var result = dbSet.Add(entity);
            TrySaveChanges();
            return result.Entity;
        }

        public IEnumerable<TEntity> AddNew(IEnumerable<TEntity> entities)
        {
            dbSet.AddRange(entities);
            TrySaveChanges();
            return entities;
        }

        public long Count()
        {
            return this.dbContextCreator().Set<TEntity>().LongCount();
        }

        public void Delete(TEntity entity)
        {
            dbSet.Remove(entity);
            TrySaveChanges();
        }

        public void DeleteAll(IEnumerable<TEntity> entities)
        {
            dbSet.RemoveRange(entities);
            TrySaveChanges();
        }

        public void DeleteById(ID id)
        {
            var pKeys = this.dbContextCreator().Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties;
            if (!pKeys.Any(k => k.Name == "Id"))
            {
                throw new PropertyNotFoundException("There is no Property named Id");
            }
            var entity = dbSet.Where("Id=@0", id).SingleOrDefault();
            if(entity!=null)
            {
                dbSet.Remove(entity);
                TrySaveChanges();
            }
        }

        public bool ExistsById(ID id)
        {
            var pKeys = this.dbContextCreator().Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties;
            if (!pKeys.Any(k => k.Name == "Id"))
            {
                throw new PropertyNotFoundException("There is no Property named Id");
            }
            return dbSet.Any("Id=@0",id);
        }

        public IEnumerable<TEntity> FindAll()
        {
            return this.dbSet.Where("Id in @0", new long[] { 1,3,5}).ToArray();
            //return this.dbContextCreator().Set<TEntity>().ToArray();
        }

        public IEnumerable<TEntity> FindAll(Sort sort)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TEntity> FindAllById(IEnumerable<ID> ids)
        {
            var pKeys = this.dbContextCreator().Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties;  
            if(!pKeys.Any(k=>k.Name=="Id"))
            {
                throw new PropertyNotFoundException("There is no Property named Id");
            }
            return dbSet.Where("Id in @0", ids).ToArray();
        }

        public TEntity FindById(ID id)
        {
            var pKeys = this.dbContextCreator().Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties;
            if (!pKeys.Any(k => k.Name == "Id"))
            {
                throw new PropertyNotFoundException("There is no Property named Id");
            }
            return dbSet.Where("Id=@0", id).SingleOrDefault();
        }

        public void Save()
        {
            this.dbContextCreator().SaveChanges();
        }
    }
}
