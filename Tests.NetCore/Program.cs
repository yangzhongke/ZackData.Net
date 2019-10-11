using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Parser;
using System.Linq.Expressions;
using YouZack.Entities;
using ZackData.NetStandard;
using ZackData.NetStandard.EF;

namespace Tests.NetCore
{
    public class Program
    {
        static void Main(string[] args)
        {
            using (YZDbContext ctx = new YZDbContext("Data Source=.;Initial Catalog=TestDB1;Integrated Security=False;User ID=sa;Password=abc@123;"))
            {
                //var stuGenerator = new RepositoryStubGenerator(() => ctx);
                //var repo = stuGenerator.Create<Book,long, IBookRepository>();
                //PageRequest page = new PageRequest { Offset=0,PageSize=10};
                //var albums = repo.FindAll(page);

                //var albums = repo.Find(e => e.Id ==1, new Sort(Order.Asc("Id"),Order.Desc(nameof(Album.Name_En))));
                //var books = repo.FindAll();
                //var books = repo.FindByAuthorId(1,new Sort(Order.Desc("Price")));
                //var books = repo.FindByAuthorId(1, new Sort(Order.Desc("Price")));
                //foreach (var album in albums.Content)

                /*
                var books1 = repo.FindByPrice(33);
                foreach (var book in books1)
                {
                    Console.WriteLine(book);
                }
                Console.WriteLine("----------------");
                var books2 = repo.FindFoo(1, "Windows 98", new Sort(Order.Desc("Price")));
                foreach (var book in books2)
                {
                    Console.WriteLine(book);
                }*/
                //var stuGenerator = new RepositoryStubGenerator(() => ctx);
                //var repo = stuGenerator.Create<Book,long, IBookRepository>();

                //var f = ctx.GetService<IQuerySqlGeneratorFactory>();
                // f.
                /*
                var parameter = Expression.Parameter(typeof(Book), "p");
                var lambdaEx = DynamicExpressionParser.ParseLambda(new ParameterExpression[] { parameter }, null, "Id=1", new object[] { });

                var f = ctx.GetService<IExpressionFragmentTranslator>();
                var ex = f.Translate(lambdaEx);
                */
                /*
                var rawSqlCommand = ctx
                    .GetService<IRawSqlCommandBuilder>()
                    .Build("select * from T_Books where Id=2", new object[0]);

                using (var dataReader = rawSqlCommand
                    .RelationalCommand
                    .ExecuteReader(
                        ctx.GetService<IRelationalConnection>(),
                        parameterValues: rawSqlCommand.ParameterValues).DbDataReader)
                {
                    while (dataReader.Read())
                    {
                        Console.WriteLine(dataReader.GetString(dataReader.GetOrdinal("Name")));
                    }
                }        */
                /*
                var books = ctx.Books.FromSql("select * from T_Books where Id<>{0}",2).OrderBy(b=>b.Price)
                    .Skip(2).Take(3).Include(b=>b.Author);
                foreach (var book in books)
                {
                    Console.WriteLine(book);
                }*/
                /*
                var items = ctx.Query<BookAuthorModel>().FromSql(@"select b.Id Id, b.Name Name, a.Name AuthorName
                from T_Books b 
                left join T_Authors a on b.AuthorId=a.Id
                where b.Price>{0}
                ",10);
                foreach(var item in items)
                {
                    Console.WriteLine(item);
                }*/
                var repo = new RepositoryGenerator2(() => ctx).Create<Book, long, IBookRepository>();
                Sort sort = new Sort(Order.Desc("Price"), Order.Asc("Id"));
                var books = repo.FindAll(sort);
                foreach (var book in books)
                {
                    Console.WriteLine(book);
                }
                //repo.Save();
                //var books1 = repo.FindAll();
                //var books1 = repo.FindFoo(3, "3", null);
                //var books1 = repo.FindAllById(new long[] { 1, 2, 5 });
                /*
                foreach (var book in books1)
                {
                    Console.WriteLine(book);
                }*/
            }                
            Console.WriteLine("ok");
            Console.Read();
        }
    }
}
