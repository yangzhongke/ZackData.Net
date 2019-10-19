using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text;
using ZackData.NetStandard.EF;
using ZackData.NetStandard.Exceptions;

namespace ZackData.NetStandard
{
    public class BaseEFCrudRepository<TEntity, ID> : ICrudRepository<TEntity, ID> where TEntity : class
    {
        protected DbContext dbCtx;

        private readonly IEntityType entityType;
        private readonly string tableName;
        private readonly IProperty[] primaryKeyProperties;

        public BaseEFCrudRepository(DbContext dbCtx)
        {
            this.dbCtx = dbCtx;
            this.entityType = dbCtx.Model.FindEntityType(typeof(TEntity));
            this.tableName = this.entityType.Relational().TableName;
            this.primaryKeyProperties = this.entityType.FindPrimaryKey().Properties.ToArray();
        }

        protected DbSet<TEntity> DbSet
        {
            get
            {
                return dbCtx.Set<TEntity>();
            }
        }

        public TEntity AddNew(TEntity entity)
        {
            var result = DbSet.Add(entity);
            SaveChanges();
            return result.Entity;
        }

        public IEnumerable<TEntity> AddNew(IEnumerable<TEntity> entities)
        {
            //todo: optimize it with SqlBulkcopy and MySqlBulkcopy
            DbSet.AddRange(entities);
            SaveChanges();
            return entities;
        }

        public long Count()
        {
            return this.dbCtx.Set<TEntity>().LongCount();
        }

        protected long Count(string predicate, params object[] args)
        {
            //https://github.com/StefH/System.Linq.Dynamic.Core/issues/311
            //LongCount is unsupported now
            return DbSet.Where(predicate, args).LongCount();
        }

        public void Delete(TEntity entity)
        {
            DbSet.Remove(entity);
            SaveChanges();
        }

        /// <summary>
        /// Translate the PropertyName of Entity Class to ColumnName of Table
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected string TranslatePropertyNameToColumnName(string propertyName)
        {
            var prop = this.entityType.FindProperty(propertyName);
            if(prop!=null)
            {
                return prop.Relational().ColumnName;
            }
            else
            {
                var property = this.entityType.GetProperties()
                    .SingleOrDefault(p => p.Relational().ColumnName.EqualsIgnoreCase(propertyName));
                if (property!=null)
                {
                    throw new ArgumentException($"Don't use FieldName-{propertyName}, please use PropertyName-{property.Name}", 
                        nameof(propertyName));
                }
                else
                {
                    throw new ArgumentException($"PropertyName {propertyName} not found", nameof(propertyName));
                }
            }
        }

        /// <summary>
        /// Execute Delete from this table
        /// </summary>
        /// <param name="whereSQL">Native SQL.don't use delete or where,please directly use the condition 'Id>5 and Age<5'</param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected int Delete(string whereSQL,params object[] args)
        {
            whereSQL = whereSQL.Trim();
            if (whereSQL.StartsWith("delete ",true,null)||whereSQL.StartsWith("where ", true, null))
            {
                throw new ArgumentException("don't use delete or where,please directly use the condition 'Id>5 and Age<5'");
            }
            //todo:use Microsoft.SqlServer.Management.SqlParser.Parser(https://www.nuget.org/packages/Microsoft.SqlServer.SqlManagementObjects/)
            //to parse whereSQL"Id>5"-->"Fid>5"
            var entityType = this.dbCtx.Model.FindEntityType(typeof(TEntity));
            string tableName = entityType.Relational().TableName;
            return ExecuteSqlCommand($"Delete from {tableName} where {whereSQL}", args);
        }

        public int DeleteAll(IEnumerable<TEntity> entities)
        {
            IEntityType eType = this.entityType;
            //Column Names of Primarykeys 
            string[] pkColNames =  this.primaryKeyProperties.Select(p=>p.Relational().ColumnName).ToArray();
            //If single primary Key,using SQL to do bulky deletion, which is more effective
            if(pkColNames.Length==1)
            {
                //Property name of Primarykeys
                string pkPropName = this.primaryKeyProperties.Single().Name;
                PropertyInfo propPK = typeof(TEntity).GetProperty(pkPropName);
                List<ID> ids = new List<ID>();//id values to be deleted
                foreach (var e in entities)
                {
                    ID id = (ID)propPK.GetValue(e);
                    ids.Add(id);
                }
                //Batch Delete SQL
                StringBuilder sbSQL = new StringBuilder();
                sbSQL.Append("delete from ").Append(this.tableName).Append(" where ")
                        .Append(pkColNames[0]).Append(" in (");
                for(int i=0;i<ids.Count;i++)
                {
                    sbSQL.Append("{").Append(i).Append("}");
                    if(i<ids.Count-1)
                    {
                        sbSQL.Append(",");
                    }
                }
                sbSQL.Append(")");
                //EFCore can convert {0}{1} to parameters to prevent SQL-Injection
                object[] args = ids.Select(e => (object)e).ToArray();
                return ExecuteSqlCommand(sbSQL.ToString(), args);
            }//if multiple primary Key, using RemoveRange
            else
            {
                DbSet.RemoveRange(entities);
                return SaveChanges();
            }      
        }

        public void DeleteById(ID id)
        {
            if (!primaryKeyProperties.Any(k => k.Name == "Id"))
            {
                throw new PropertyNotFoundException("There is no Property named Id");
            }
            var entity = DbSet.Where("Id=@0", id).SingleOrDefault();
            if(entity!=null)
            {
                DbSet.Remove(entity);
                SaveChanges();
            }
        }

        public bool ExistsById(ID id)
        {
            if (!primaryKeyProperties.Any(k => k.Name == "Id"))
            {
                throw new PropertyNotFoundException("There is no Property named Id");
            }
            return DbSet.Any("Id=@0",id);
        }

        public IQueryable<TEntity> FindAll()
        {
            return this.DbSet;
        }

        public IQueryable<TEntity> FindAll(Order[] orders)
        {
            return this.Find(orders, null);
        }

        public IQueryable<TEntity> FindAllById(IEnumerable<ID> ids)
        {
            var pKeys = this.dbCtx.Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties;  
            if(!pKeys.Any(k=>k.Name=="Id"))
            {
                throw new PropertyNotFoundException("There is no Property named Id");
            }
            return DbSet.Where("Id in @0", ids);
        }

        public TEntity FindById(ID id)
        {
            var pKeys = this.dbCtx.Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties;
            if (!pKeys.Any(k => k.Name == "Id"))
            {
                throw new PropertyNotFoundException("There is no Property named Id");
            }
            return DbSet.Where("Id=@0", id).SingleOrDefault();
        }
        
        public IQueryable<TEntity> Find(string predicate, params object[] args)
        {
            if(string.IsNullOrWhiteSpace(predicate))
            {
                return this.DbSet;
            }
            else
            {
                return DbSet.Where(predicate, args);
            }
        }

        public TEntity FindOne(string predicate, params object[] args)
        {
            return DbSet.Where(predicate, args).SingleOrDefault();
        }

        public IQueryable<TEntity> Find(Order order, string predicate, params object[] args)
        {
            return Find(new Order[] { order }, predicate, args);
        }
        public IQueryable<TEntity> Find(Order[] orders, string predicate, params object[] args)
        {
            IQueryable<TEntity> result;
            if(string.IsNullOrWhiteSpace(predicate))
            {
                result = this.DbSet;
            }
            else
            {
                result = this.DbSet.Where(predicate, args);
            }
            if (orders != null && orders.Length > 0)
            {
                var firstOrder = orders.First();
                var orderedResult = Helper.OrderBy(result, firstOrder.Property, firstOrder.Ascending);
                foreach (var order in orders.Skip(1))
                {
                    orderedResult = Helper.ThenBy(orderedResult, order.Property, order.Ascending);
                }
                result = orderedResult;
            }
            return result;
        }

        public Page<TEntity> Find(PageRequest pageRequest, string predicate, params object[] args)
        {
            if(pageRequest==null)
            {
                throw new ArgumentNullException(nameof(pageRequest));
            }
            Page<TEntity> page = new Page<TEntity>();

            IQueryable<TEntity> result;
            if(string.IsNullOrWhiteSpace(predicate))
            {
                result = this.DbSet;
            }
            else
            {
                result = this.DbSet.Where(predicate, args);
            }
            
            var orders = pageRequest.Orders;
            if (orders != null && orders.Length > 0)
            {
                var firstOrder = orders.First();
                var orderedResult = Helper.OrderBy(result, firstOrder.Property, firstOrder.Ascending);
                foreach (var order in orders.Skip(1))
                {
                    orderedResult = Helper.ThenBy(orderedResult, order.Property, order.Ascending);
                }
                result = orderedResult;
            }

            //Calculate the totalCount 
            long totalCount = result.LongCount();
            //do pageing,query current data of page
            result = result.Skip(pageRequest.PageNumber * pageRequest.PageSize).Take(pageRequest.PageSize);

            page.Content = result;
            page.TotalElements = totalCount;
            page.PageNumber = pageRequest.PageNumber;
            page.PageSize = pageRequest.PageSize;
            page.TotalPages = (int)Math.Ceiling(totalCount*1.0 / pageRequest.PageSize);
            return page;
        }

        protected int SaveChanges()
        {
            return this.dbCtx.SaveChanges();
        }

        protected IEnumerable<TEntity> FromSQL(string sql, params object[] args)
        {
            //FromSql:Install-Package Microsoft.EntityFrameworkCore.Relational -Version 2.2.0
            return DbSet.FromSql(sql, args).ToArray();
        }

        protected int ExecuteSqlCommand(string sql, params object[] args)
        {
            //ExecuteSqlCommand:Install-Package Microsoft.EntityFrameworkCore.Relational -Version 2.2.0
            return this.dbCtx.Database.ExecuteSqlCommand(sql, args);
        }
    }
}
