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
using System.Reflection;

namespace ZackData.NetStandard.EF
{
    class RepositoryInterceptor<TEntity,ID> : IInterceptor where TEntity:class
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
            //var repositoryType = invocation.Method.DeclaringType;
            var entityType = typeof(TEntity);
            var idType = typeof(ID);

            var dbCtx = dbContextCreator();
            var dbSet = dbCtx.Set<TEntity>();
            
            if (methodName == nameof(ICrudRepository<int, int>.AddNew))
            {
                var firstParamType = parameters[0].ParameterType;
                //IEnumerable<T> AddNew(IEnumerable<T> entities);
                if (typeof(IEnumerable<TEntity>)==firstParamType)
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
                    dbCtx.SaveChanges();
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
                    dbCtx.SaveChanges();
                }
                //void Delete(T entity);
                else if (methodName==nameof(ICrudRepository<TEntity, int>.Delete))
                {
                    object entity = argumentValues[0];
                    dbCtx.Remove(entity);
                    dbCtx.SaveChanges();
                }
                //void DeleteAll(IEnumerable<T> entities);
                else if (methodName == nameof(ICrudRepository<int, int>.DeleteAll))
                {
                    IEnumerable entities = (IEnumerable)argumentValues[0];
                    dbCtx.RemoveRange(entities);
                    dbCtx.SaveChanges();
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
                System.Linq.IQueryable<TEntity> result = dbSet;
                Sort sort = FindSingleParameterOfType<Sort>(invocation);
                if (sort != null)
                {
                    ParameterExpression eParameterExpr = Expression.Parameter(typeof(TEntity), "e");

                    foreach (var order in sort.Orders)
                    {
                        result = OrderBy(result, order.Property, order.Ascending);
                    }
                }

                var queryAttr = invocation.Method.GetCustomAttribute<QueryAttribute>();
                //if QueryAttribute is marked in the method,
                //ignore all the naming convetion
                if (queryAttr!=null)
                {
                    string queryExpression = queryAttr.QueryExpression;
                    object[] normalArgumentValues = GetNormalArgumentValues(invocation);
                    result = result.Where(queryExpression, normalArgumentValues);
                    //https://github.com/StefH/System.Linq.Dynamic.Core/wiki/Dynamic-Expressions
                    //result = result.Where("Id!=@0", aa).OrderBy("Price");
                }
                else//following the naming convetion
                {
                    if(methodName.StartsWith("FindBy"))
                    {
                        object[] normalArgumentValues = GetNormalArgumentValues(invocation);
                        string propertyName = methodName.Substring("FindBy".Length);
                        result = result.Where($"{propertyName}=@0", normalArgumentValues[0]);
                    }
                }
                invocation.ReturnValue = result.ToArray();

                //FindByName(string name)
                //FindByAlbumId(long albumId,PageRequest pageRequest);
                //FindByName(string name)
            }
            else
            {
                throw new NotImplementedException(methodName);
            }
        }

        /// <summary>
        /// Get argumentValues(order kept) except the type of Order and PageRequest
        /// </summary>
        /// <param name="invocation"></param>
        /// <returns></returns>
        static object[] GetNormalArgumentValues(IInvocation invocation)
        {
            object[] argumentValues = invocation.Arguments;
            var parameters = invocation.Method.GetParameters();

            List<object> values = new List<object>();
            for(int i= 0;i<parameters.Length;i++)
            {
                var parameter = parameters[i];
                if(parameter.ParameterType==typeof(Order)|| parameter.ParameterType == typeof(PageRequest))
                {
                    continue;
                }
                values.Add(argumentValues[i]);
            }
            return values.ToArray();
        }

        public static IQueryable<TEntity> OrderBy(IQueryable<TEntity> source, string sortProperty, bool isAscending)
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
