using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq;
using System;
using System.Collections.Generic;
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
                /*
                ctx.Add(new Book { AuthorId=2,Name="Java1",Price=56.2,PublishDate=DateTime.Now});
                ctx.SaveChanges();*/
                /*
                foreach(var b in ctx.Books.Include(b=>b.Author))
                {
                    Console.WriteLine(b);
                }*/
                BaseEFCrudRepository<Book, long> rep = new BaseEFCrudRepository<Book, long>(()=>ctx);
                var books = rep.Find("Id>=13");
                rep.DeleteAll(books);
            }                
            Console.WriteLine("ok");
            Console.Read();
        }
    }
}
