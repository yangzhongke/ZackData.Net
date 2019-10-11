using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Linq.Expressions;

namespace ZackData.NetStandard.EF
{
    public static class Helper
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

            if (type.IsArray)
            {
                sbCode.Append("[]");
            }
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
