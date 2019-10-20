using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection;
using ZackData.NetStandard;
using ZackData.NetStandard.EF;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RepositoriesServiceCollectionExtensions
    {
        /// <summary>
        /// Register all the interfaces that implement ICrudRepository in assemblies,
        /// one instance of stub Repository  for one request
        /// </summary>
        /// <param name="services"></param>
        /// <param name="dbContextCreator">delegate to create an instance of DbSet</param>
        /// <param name="assemblies">assemblies that contain Repository Interfaces</param>
        /// <returns></returns>
        public static IServiceCollection AddScopedRepositories(this IServiceCollection services,
            Func<DbContext> dbContextCreator, params Assembly[] assemblies)
        {
            RepositoryStubGenerator repositoryStubGenerator = new RepositoryStubGenerator();
            foreach (var repType in assemblies.SelectMany(t=>t.GetTypes())
                .Where(t => HasImplementedRawGeneric(t, typeof(ICrudRepository<,>))))
            {
                services.AddScoped(repType, sp =>
                {
                    MethodInfo methodGenericCreate = GetCreateMethod(repType);
                    var dbCtx = dbContextCreator();
                    return methodGenericCreate.Invoke(repositoryStubGenerator, new object[] { dbCtx });
                });
            }
            return services;
        }

        /// <summary>
        /// Register all the interfaces that implement ICrudRepository in assemblies,
        /// single instance of stub Repository
        /// </summary>
        /// <param name="services"></param>
        /// <param name="dbContextCreator">delegate to create an instance of DbSet</param>
        /// <param name="assemblies">assemblies that contain Repository Interfaces</param>
        /// <returns></returns>
        public static IServiceCollection AddSingletonRepositories(this IServiceCollection services,
            Func<DbContext> dbContextCreator, params Assembly[] assemblies)
        {
            RepositoryStubGenerator repositoryStubGenerator = new RepositoryStubGenerator();
            foreach (var repType in assemblies.SelectMany(t => t.GetTypes())
                .Where(t => HasImplementedRawGeneric(t, typeof(ICrudRepository<,>))))
            {
                services.AddSingleton(repType, sp =>
                {
                    MethodInfo methodGenericCreate = GetCreateMethod(repType);
                    var dbCtx = dbContextCreator();
                    return methodGenericCreate.Invoke(repositoryStubGenerator, new object[] { dbCtx });
                });
            }
            return services;
        }

        /// <summary>
        /// Register all the interfaces that implement ICrudRepository in assemblies,
        /// each instance of stub Repository for each IOC
        /// </summary>
        /// <param name="services"></param>
        /// <param name="dbContextCreator">delegate to create an instance of DbSet</param>
        /// <param name="assemblies">assemblies that contain Repository Interfaces</param>
        /// <returns></returns>
        public static IServiceCollection AddTransientRepositories(this IServiceCollection services,
            Func<DbContext> dbContextCreator, params Assembly[] assemblies)
        {
            RepositoryStubGenerator repositoryStubGenerator = new RepositoryStubGenerator();
            foreach (var repType in assemblies.SelectMany(t => t.GetTypes())
                .Where(t => HasImplementedRawGeneric(t, typeof(ICrudRepository<,>))))
            {
                services.AddTransient(repType, sp =>
                {
                    MethodInfo methodGenericCreate = GetCreateMethod(repType);
                    var dbCtx = dbContextCreator();
                    return methodGenericCreate.Invoke(repositoryStubGenerator, new object[] { dbCtx });
                });
            }
            return services;
        }

        /// <summary>
        /// get the generic 'Create' method of RepositoryStubGenerator with <TEntity, ID, TRepository>
        /// from RepositoryType 
        /// </summary>
        /// <param name="repositoryType"></param>
        /// <returns></returns>
        static MethodInfo GetCreateMethod(Type repositoryType)
        {
            Type typeCrudRepository = FindICrudRepositoryType(repositoryType);
            var crudRepositoryGenericArguments = typeCrudRepository.GetGenericArguments();
            //Get TEntity And ID from the parent interface ICrudRepository of repositoryType
            var typeEntityType = crudRepositoryGenericArguments[0];
            var typeId = crudRepositoryGenericArguments[1];

            //Create<TEntity, ID, TRepository>
            MethodInfo methodCreate = typeof(RepositoryStubGenerator).GetMethod(nameof(RepositoryStubGenerator.Create));
            MethodInfo methodGenericCreate = methodCreate.MakeGenericMethod(typeEntityType, typeId, repositoryType);
            return methodGenericCreate;
        }

        /// <summary>
        /// Find ICrudRepository types from the parent interfaces of type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static Type FindICrudRepositoryType(Type type)
        {
            List<Type> intfTypes = new List<Type>();
            Helper.GetAllParentInterface(type, intfTypes);
            foreach (var intfType in intfTypes)
            {
                if (intfType.IsGenericType
                    && typeof(ICrudRepository<,>).IsAssignableFrom(intfType.GetGenericTypeDefinition()))
                {
                    return intfType;
                }
            }
            return null;
        }

        /// <summary>
        /// Is <paramref name="type"/> a sub-interface of type  genericType。
        /// and genericType is a generic inteface
        /// </summary>
        static bool HasImplementedRawGeneric(Type type, Type genericType)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (genericType == null) throw new ArgumentNullException(nameof(genericType));

            Func<Type, bool> funcIsTheRawGenericType = (test) =>
            {
                return genericType == (test.IsGenericType ? test.GetGenericTypeDefinition() : test);
            };

            var isTheRawGenericType = type.GetInterfaces().Any(funcIsTheRawGenericType);
            if (isTheRawGenericType) return true;

            while (type != null && type != typeof(object))
            {
                isTheRawGenericType = funcIsTheRawGenericType(type);
                if (isTheRawGenericType) return true;
                type = type.BaseType;
            }
            // not found
            return false;
        }
    }
}
