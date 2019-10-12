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
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;
using System.Text;

namespace ZackData.NetStandard
{
    public class BaseEFCrudRepository<TEntity, ID> : ICrudRepository<TEntity, ID> where TEntity : class
    {
        protected Func<DbContext> dbContextCreator;

        private IEntityType entityType;
        private string tableName;
        private IProperty[] pkProperties;

        public BaseEFCrudRepository(Func<DbContext> dbContextCreator)
        {
            this.dbContextCreator = dbContextCreator;
            this.entityType = dbContextCreator().Model.FindEntityType(typeof(TEntity));
            this.tableName = this.entityType.Relational().TableName;
            this.pkProperties = this.entityType.FindPrimaryKey().Properties.ToArray();
        }

        protected DbSet<TEntity> DbSet
        {
            get
            {
                return this.dbContextCreator().Set<TEntity>();
            }
        }
            
        protected IEntityType EntityType
        {
            get
            {
                return this.entityType;
            }
        }

        protected string TableName
        {
            get
            {
                return this.tableName;
            }
        }

        protected IProperty[] PrimaryKeyProperties
        {
            get
            {
                return this.pkProperties;
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
            DbSet.AddRange(entities);
            SaveChanges();
            return entities;
        }

        public long Count()
        {
            return this.dbContextCreator().Set<TEntity>().LongCount();
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
            var prop = EntityType.FindProperty(propertyName);
            if(prop!=null)
            {
                return prop.Relational().ColumnName;
            }
            else
            {
                var property = EntityType.GetProperties()
                    .SingleOrDefault(p => p.Relational().ColumnName.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
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

        protected int Delete(string whereSQL,params object[] args)
        {
            whereSQL = whereSQL.Trim();
            if (whereSQL.StartsWith("delete ",true,null)||whereSQL.StartsWith("where ", true, null))
            {
                throw new ArgumentException("don't use delete or where,please directly use the condition 'Id>5 and Age<5'");
            }
            //todo:use Microsoft.SqlServer.Management.SqlParser.Parser(https://www.nuget.org/packages/Microsoft.SqlServer.SqlManagementObjects/)
            //to parse whereSQL"Id>5"-->"Fid>5"
            var entityType = dbContextCreator().Model.FindEntityType(typeof(TEntity));
            string tableName = entityType.Relational().TableName;
            return ExecuteSqlCommand($"Delete from {tableName} where {whereSQL}", args);
        }

        public int DeleteAll(IEnumerable<TEntity> entities)
        {
            IEntityType eType = this.EntityType;
            //Column Names of Primarykeys 
            string[] pkColNames =  this.PrimaryKeyProperties.Select(p=>p.Relational().ColumnName).ToArray();
            //If single primary Key,using SQL to delete

            //int,long,guid
            if(pkColNames.Length==1)
            {
                //Property name of Primarykeys
                string pkPropName = this.PrimaryKeyProperties.Single().Name;
                PropertyInfo propPK = typeof(TEntity).GetProperty(pkPropName);
                List<ID> ids = new List<ID>();//id values to be deleted
                foreach (var e in entities)
                {
                    ID id = (ID)propPK.GetValue(e);
                    ids.Add(id);
                }
                //Batch Delete SQL
                StringBuilder sbSQL = new StringBuilder();
                sbSQL.Append("delete from ").Append(this.TableName).Append(" where ")
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
                return ExecuteSqlCommand(sbSQL, args);
            }//if multiple primary Key, using RemoveRange
            else
            {
                DbSet.RemoveRange(entities);
                return SaveChanges();
            }      
        }

        public void DeleteById(ID id)
        {
            if (!PrimaryKeyProperties.Any(k => k.Name == "Id"))
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
            if (!PrimaryKeyProperties.Any(k => k.Name == "Id"))
            {
                throw new PropertyNotFoundException("There is no Property named Id");
            }
            return DbSet.Any("Id=@0",id);
        }

        public IEnumerable<TEntity> FindAll()
        {
            return this.DbSet.ToArray();
        }

        public IEnumerable<TEntity> FindAll(Sort sort)
        {
            IQueryable<TEntity> result = this.DbSet;
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
            return DbSet.Where("Id in @0", ids).ToArray();
        }

        public TEntity FindById(ID id)
        {
            var pKeys = this.dbContextCreator().Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties;
            if (!pKeys.Any(k => k.Name == "Id"))
            {
                throw new PropertyNotFoundException("There is no Property named Id");
            }
            return DbSet.Where("Id=@0", id).SingleOrDefault();
        }
        
        public IEnumerable<TEntity> Find(string predicate, params object[] args)
        {
            return DbSet.Where(predicate, args).ToArray();
        }

        public TEntity FindOne(string predicate, params object[] args)
        {
            return DbSet.Where(predicate, args).SingleOrDefault();
        }

        public IEnumerable<TEntity> Find(Sort sort, string predicate, params object[] args)
        {
            throw new NotImplementedException();
        }

        public Page<TEntity> Find(PageRequest pageRequest, Sort sort, string predicate, params object[] args)
        {
            throw new NotImplementedException();
        }

        protected int SaveChanges()
        {
            return this.dbContextCreator().SaveChanges();
        }

        protected IEnumerable<TEntity> FromSQL(string sql, params object[] args)
        {
            //FromSql:Install-Package Microsoft.EntityFrameworkCore.Relational -Version 2.2.0
            return DbSet.FromSql(sql, args).ToArray();
        }

        protected int ExecuteSqlCommand(string sql, params object[] args)
        {
            //ExecuteSqlCommand:Install-Package Microsoft.EntityFrameworkCore.Relational -Version 2.2.0
            return dbContextCreator().Database.ExecuteSqlCommand(sql, args);
        }

        protected int ExecuteSqlCommand(StringBuilder sql, params object[] args)
        {
            return ExecuteSqlCommand(sql.ToString(), args);
        }
    }
}
