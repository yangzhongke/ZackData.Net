using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZackData.NetStandard.EF
{
    public class RepositoryStubGenerator
    {
        private Func<DbContext> dbContextCreator;
        public RepositoryStubGenerator(Func<DbContext> dbContextCreator)
        {
            this.dbContextCreator = dbContextCreator;
        }

        /// <summary>
        /// The method is for reflection
        /// </summary>
        /// <param name="typeEntity"></param>
        /// <param name="typeID"></param>
        /// <param name="typeRepository"></param>
        /// <returns></returns>
        public object Create(Type typeEntity, Type typeID, Type typeRepository)
        {
            //methodCreate links to public TRepository Create<TEntity, ID, TRepository>()
            var methodCreate = GetType().GetMethod(nameof(Create), new Type[0]);
            methodCreate = methodCreate.MakeGenericMethod(typeEntity, typeID,typeRepository);
            return methodCreate.Invoke(this, new object[0]);
        }

        public TRepository Create<TEntity, ID, TRepository>() where TEntity : class
            where TRepository:class
        {
            ProxyGenerator generator = new ProxyGenerator();
            IInterceptor interceptor = new RepositoryInterceptor<TEntity,ID>(this.dbContextCreator);
            var h = generator.CreateInterfaceProxyWithoutTarget<TRepository>(interceptor);
            return h;
        }
    }
}
