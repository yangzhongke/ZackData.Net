﻿using System;
using System.Text.RegularExpressions;
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
                RepositoryStubGenerator gen = new RepositoryStubGenerator(() => ctx);
                var rep = gen.Create<Book,long,IBookRepository>();
                //var rep = new BaseEFCrudRepository<Book, long>(() => ctx);

                var books = rep.FindFoo(1, "3About Microsoft", Order.Asc("Priace"));
                foreach(var b in books)
                {
                    Console.WriteLine(b);
                }
                Console.WriteLine("ok");
                /*
                PageRequest pageReq = new PageRequest { PageNumber = 1, PageSize = 3, 
                    Sort = new Sort(Order.Desc("Price")) };
                PageRequest pageReq = new PageRequest
                {
                    PageNumber = 1,
                    PageSize = 3
                };
                var page = rep.Find(pageReq, "Price>5");
                
                foreach (var b in page.Content)
                {
                    Console.WriteLine(b);
                }

                Console.WriteLine(page.TotalElements);
                Console.WriteLine(page.PageNumber);
                Console.WriteLine(page.PageSize);
                Console.WriteLine(page.TotalPages);*/
            }
            
            Console.Read();
        }
    }
}
