using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq;
using System.Linq;

namespace Tests.NetCore
{
    public static class IQueryableExtensions
    {
        public static string ToSql<TEntity>(this IQueryable<TEntity> query,DbContext dbCtx)
        {
            IQueryCompiler queryCompiler = dbCtx.GetService<IQueryCompiler>();
            IQueryModelGenerator modelGenerator = dbCtx.GetService<IQueryModelGenerator>();
            QueryModel queryModel = modelGenerator.ParseQuery(query.Expression);
            IDatabase database = dbCtx.GetService<IDatabase>();
            DatabaseDependencies databaseDependencies = dbCtx.GetService<DatabaseDependencies>();
            QueryCompilationContext queryCompilationContext = databaseDependencies.QueryCompilationContextFactory.Create(false);
            RelationalQueryModelVisitor modelVisitor = (RelationalQueryModelVisitor)queryCompilationContext.CreateQueryModelVisitor();
            modelVisitor.CreateQueryExecutor<TEntity>(queryModel);
            var sql = modelVisitor.Queries.First().ToString();            
            return sql;
        }
    }
}
