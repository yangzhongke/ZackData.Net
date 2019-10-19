using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using ZackData.NetStandard.Exceptions;

namespace ZackData.NetStandard.EF
{
    static class Helper
    {
        public static void GetAllParentInterface(Type type,List<Type> interfaces)
        {
            interfaces.AddRange(type.GetInterfaces());
            foreach(var intyptf in type.GetInterfaces())
            {
                GetAllParentInterface(intyptf, interfaces);
            }
        }

        public static string CreateCodeFromType(Type type)
        {
            //IEnumerable`1  --> IEnumerable
            StringBuilder sbCode = new StringBuilder(type.Namespace+"."+type.Name.Split('`')[0]);
           /*
            if (type.IsArray&&type.GetArrayRank()==1)
            {
                sbCode.Append("[]");
            }*/
            var genericTypeNames = type.GenericTypeArguments.Select(t=>t.Namespace+"."+t.Name);
            if(genericTypeNames.Any())
            {
                sbCode.Append("<").Append(string.Join(",", genericTypeNames)).Append(">");
            }
            return sbCode.ToString();
        }

        public static string CreateCodeFromMethodDelaration(MethodInfo method)
        {
            StringBuilder sbCode = new StringBuilder();
            sbCode.Append("public ").Append(CreateCodeFromType(method.ReturnType)).Append(" ").Append(method.Name);
            sbCode.Append("(");
            List<string> argsCode = new List<string>();
            foreach(var parameter in method.GetParameters())
            {
                argsCode.Add(CreateCodeFromType(parameter.ParameterType)+" "+parameter.Name);
            }
            sbCode.Append(string.Join(",", argsCode));
            sbCode.Append(")");
            return sbCode.ToString();
        }

        /*
        /// <summary>
        /// Get argumentValues(order kept) except for the type of Order,Sort and PageRequest
        /// </summary>
        /// <param name="invocation"></param>
        /// <returns></returns>
        static ParameterInfo[] GetPlainParameters(MethodInfo method)
        {
            var parameters = method.GetParameters();
            return parameters.Where(p => p.ParameterType != typeof(Sort) && p.ParameterType != typeof(Order)
            && p.ParameterType != typeof(PageRequest)).ToArray();
        }  */

        /// <summary>
        ///  Try to find a parameterInfo of type "type",
        /// if not find one, return null,
        /// only zero or one parameter of the type is allowed, if more than one are found, Exception will be thrown
        /// </summary>
        /// <typeparam name="TParameter"></typeparam>
        /// <param name="method"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ParameterInfo FindSingleParameterOfType(MethodInfo method,Type type)
        {
            var parameters = method.GetParameters();
            int paramIndex = -1;
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.ParameterType == type)
                {
                    //another parameter of type TParameter is found
                    if (paramIndex >= 0)
                    {
                        throw new ConventionException($"only zero or one parameter of the type {type} is allowed");
                    }
                    paramIndex = i;
                }
            }
            if (paramIndex >= 0)
            {
                return parameters[paramIndex];
            }
            else
            {
                return null;
            }
        }

        public static IOrderedQueryable<TEntity> OrderBy<TEntity>(IQueryable<TEntity> source, string sortProperty, bool isAscending)
        {
            var type = typeof(TEntity);
            var property = type.GetProperty(sortProperty);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExp = Expression.Lambda(propertyAccess, parameter);
            var typeArguments = new Type[] { type, property.PropertyType };
            var methodName = isAscending ? "OrderBy" : "OrderByDescending";
            var resultExp = Expression.Call(typeof(Queryable), methodName, typeArguments, source.Expression, Expression.Quote(orderByExp));

            return (IOrderedQueryable<TEntity>)source.Provider.CreateQuery<TEntity>(resultExp);
        }

        public static IOrderedQueryable<TEntity> ThenBy<TEntity>(IOrderedQueryable<TEntity> source, string sortProperty, bool isAscending)
        {
            var type = typeof(TEntity);
            var property = type.GetProperty(sortProperty);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var thenByExp = Expression.Lambda(propertyAccess, parameter);
            var typeArguments = new Type[] { type, property.PropertyType };
            var methodName = isAscending ? "ThenBy" : "ThenByDescending";
            var resultExp = Expression.Call(typeof(Queryable), methodName, typeArguments, source.Expression, Expression.Quote(thenByExp));

            return (IOrderedQueryable<TEntity>)source.Provider.CreateQuery<TEntity>(resultExp);
        }
    }
}
