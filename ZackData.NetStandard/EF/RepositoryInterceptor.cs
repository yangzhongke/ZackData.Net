using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
//using System.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
//using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

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
                else if (methodName == nameof(ICrudRepository<int, int>.DeleteAll))
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
                /*
                if(methodName== nameof(ICrudRepository<int, int>.FindAll)
                    && argumentValues.Length==0)
                {
                    invocation.ReturnValue = dbSet.ToArray();
                }
                //Page<T> FindAll(PageRequest pageRequest);
                else if (methodName == nameof(ICrudRepository<int, int>.FindAll)
                    && parameters.Length==1
                    &&typeof(PageRequest)== parameters[0].ParameterType)
                {
                    PageRequest pageRequest = (PageRequest)argumentValues[0];

                    var items = dbSet.Skip(pageRequest.Offset).Take(pageRequest.PageSize);
                    Page<TEntity> page = new Page<TEntity>();
                    page.Content = items;
                    invocation.ReturnValue = page;
                }
                //IEnumerable<T> Find();
                //Page<T> Find(PageRequest pageRequest);
                //IEnumerable<T> Find(Func<TEntity, bool> where, Sort sort);
                //IEnumerable<T> Find(Func<TEntity, bool> where,PageRequest pageRequest);
                //IEnumerable<T> Find(Func<TEntity, bool> where,Sort sort);
                //Page<T> Find(Func<TEntity, bool> where,PageRequest pageRequest, Sort sort);
                //Page<T> Find(PageRequest pageRequest);
                */
                if (methodName =="FindAll")
                {
                    System.Linq.IQueryable<TEntity> result = dbSet;
                    Sort sort = FindSingleParameterOfType<Sort>(invocation);
                    if (sort != null)
                    {
                        ParameterExpression eParameterExpr = Expression.Parameter(typeof(TEntity), "e");

                        foreach (var order in sort.Orders)
                        {
                            result = OrderBy(result, order.Property, order.Ascending);
                        }
                        //result = result.OrderBy()
                    }
                    if(methodName=="FindAll")
                    {
                        int aa = 2;
                        //https://github.com/StefH/System.Linq.Dynamic.Core/wiki/Dynamic-Expressions
                        result = result.Where("Id!=@0",aa).OrderBy("Price");
                    }
                    invocation.ReturnValue = result.ToArray();
                }
                else
                {
                    throw new NotImplementedException(methodName);
                }
                
                
                //FindByName(string name)
                //FindByAlbumId(long albumId,PageRequest pageRequest);
                //FindByName(string name)
            }
            else
            {
                throw new NotImplementedException(methodName);
            }
        }

        public static System.Linq.IQueryable<TEntity> OrderBy(System.Linq.IQueryable<TEntity> source, string sortProperty, bool isAscending)
        {
            var type = typeof(TEntity);
            var property = type.GetProperty(sortProperty);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExp = Expression.Lambda(propertyAccess, parameter);
            var typeArguments = new Type[] { type, property.PropertyType };
            var methodName = isAscending ? "OrderBy" : "OrderByDescending";
            var resultExp = Expression.Call(typeof(Queryable), methodName, typeArguments, source.Expression, Expression.Quote(orderByExp));

            return source.Provider.CreateQuery<TEntity>(resultExp);
        }

        /// <summary>
        /// Try to find a parameter of type "TParameter",
        /// if not find one, return null,
        /// only zero or one parameter of the type is allowed, if more than one are found, Exception will be thrown
        /// </summary>
        /// <param name="invocation"></param>
        /// <returns></returns>
        static TParameter FindSingleParameterOfType<TParameter>(IInvocation invocation) where TParameter:class
        {
            var parameters = invocation.Method.GetParameters();
            int paramIndex = -1;
            for (int i=0;i<parameters.Length;i++)
            {
                var parameter = parameters[i];
                if(parameter.ParameterType == typeof(TParameter))
                {
                    //another parameter of type TParameter is found
                    if (paramIndex>=0)
                    {
                        throw new ArgumentException($"only zero or one parameter of the type {typeof(TParameter)} is allowed");
                    }
                    paramIndex = i;
                }
            }
            if(paramIndex>=0)
            {
                return (TParameter)invocation.Arguments[paramIndex];
            }
            else
            {
                return null;
            }
        }
    }
}
