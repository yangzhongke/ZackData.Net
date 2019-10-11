using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using ZackData.NetStandard.EF;
using ZackData.NetStandard.Exceptions;
using System.Linq.Dynamic.Core.Parser;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
            IQueryable<TEntity> result = this.dbSet;
            if (sort != null && sort.Orders.Count > 0)
            {
                var firstOrder = sort.Orders.First();
                var orderedResult = Helper.OrderBy(result, firstOrder.Property, firstOrder.Ascending);
                foreach (var order in sort.Orders.Skip(1))
                {
                    orderedResult = Helper.ThenBy(orderedResult, order.Property, order.Ascending);
                }
                result = orderedResult;
            }
            return result.ToArray();
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

            // this.dbContextCreator().SaveChanges();
        }

        public IEnumerable<TEntity> Find(string predicate, params object[] args)
        {
            return dbSet.Where(predicate, args).ToArray();
        }

        public TEntity FindOne(string predicate, params object[] args)
        {
            return dbSet.Where(predicate, args).SingleOrDefault();
        }

        public long Count(string predicate, params object[] args)
        {
            //https://github.com/StefH/System.Linq.Dynamic.Core/issues/311
            //LongCount is unsupported now
            return dbSet.Where(predicate, args).LongCount();
        }

        public IEnumerable<TEntity> Find(Sort sort, string predicate, params object[] args)
        {
            throw new NotImplementedException();
        }

        public Page<TEntity> Find(PageRequest pageRequest, Sort sort, string predicate, params object[] args)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TEntity> FromSQL(string sql, params object[] args)
        {
            //FromSql:Install-Package Microsoft.EntityFrameworkCore.Relational -Version 2.2.0
            return dbSet.FromSql(sql, args).ToArray();
        }

        public int ExecuteSqlCommand(string sql, params object[] args)
        {
            //ExecuteSqlCommand:Install-Package Microsoft.EntityFrameworkCore.Relational -Version 2.2.0
            return dbContextCreator().Database.ExecuteSqlCommand(sql, args);
        }
    }
}
