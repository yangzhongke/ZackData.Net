using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Linq;
using Microsoft.EntityFrameworkCore.Query;
using System.Collections.Generic;

namespace ZackData.NetStandard.EF
{
    class RepositoryInterceptor<TEntity> : IInterceptor where TEntity:class
    {
        private Func<DbContext> dbContextCreator;
        public RepositoryInterceptor(Func<DbContext> dbContextCreator)
        {
            this.dbContextCreator = dbContextCreator;
        }

        //todo，同时支持同步和异步方法

        public void Intercept(IInvocation invocation)
        {
            //todo: Cached
            string methodName = invocation.Method.Name;            
            object[] argumentValues = invocation.Arguments;
            var parameters = invocation.Method.GetParameters();
            var repositoryType = invocation.Method.DeclaringType;
            var entityType = repositoryType.GenericTypeArguments[0];
            var idType = repositoryType.GenericTypeArguments[1];

            var dbCtx = dbContextCreator();
            var dbSet = dbCtx.Set<TEntity>();
            
            //void Save();
            if (methodName==nameof(ICrudRepository<int,int>.Save))
            {
                dbCtx.SaveChanges();
            }
            else if (methodName == nameof(ICrudRepository<int, int>.AddNew))
            {
                var firstParamType = parameters[0].ParameterType;
                //IEnumerable<T> AddNew(IEnumerable<T> entities);
                if (typeof(IEnumerable<TEntity>).IsAssignableFrom(firstParamType))
                {
                    IEnumerable<TEntity> entities = (IEnumerable<TEntity>)argumentValues[0];
                    dbCtx.AddRange(entities);
                    dbCtx.SaveChanges();
                    invocation.ReturnValue = entities;
                }
                else//T AddNew(T entity);
                {                    
                    object entity = argumentValues[0];
                    dbCtx.Add(entity);                    
                    invocation.ReturnValue = argumentValues[0];
                }                
            }
            else if(methodName.StartsWith("Delete"))
            {
                //void DeleteById(ID id);
                if (methodName== nameof(ICrudRepository<TEntity,int>.DeleteById))
                {
                    object id = argumentValues[0];
                    object entity = dbCtx.Find(entityType, id);
                    dbCtx.Remove(entity);
                }
                //void Delete(T entity);
                else if (methodName==nameof(ICrudRepository<TEntity, int>.Delete))
                {
                    object entity = argumentValues[0];
                    dbCtx.Remove(entity);
                }
                //void DeleteAll(IEnumerable<T> entities);
                else if (methodName == nameof(ICrudRepository<TEntity, int>.DeleteAll))
                {
                    IEnumerable entities = (IEnumerable)argumentValues[0];
                    dbCtx.RemoveRange(entities);
                }
                else
                {
                    //DeleteByName
                    throw new NotImplementedException("todo");
                }
            }
            else if(methodName.StartsWith("Find"))
            {
                //IEnumerable<T> FindAll();
                if(argumentValues.Length==0)
                {
                    invocation.ReturnValue = dbSet.ToArray();
                }
                else if(parameters.Length==1
                    &&typeof(PageRequest).IsAssignableFrom(parameters[0].ParameterType))
                {
                    PageRequest pageRequest = (PageRequest)argumentValues[0];

                    var items = dbSet.Skip(pageRequest.Offset).Take(pageRequest.PageSize);
                    Page<TEntity> page = new Page<TEntity>();
                    page.Content = items;
                    invocation.ReturnValue = page;
                }
                else
                {
                    throw new NotImplementedException(methodName);
                }
                //Page<T> FindAll(PageRequest pageRequest);
                //IEnumerable<T> Find(Predicate<T> where, Sort sort = null);
                //FindByName(string name)
                //FindByAlbumId(long albumId,PageRequest pageRequest);
                //FindByName(string name)
            }
            else
            {
                throw new NotImplementedException(methodName);
            }
        }
    }
}
