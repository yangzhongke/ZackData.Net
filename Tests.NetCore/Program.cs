using System;
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
                //var books = rep.Find("Name.Contains(\"m\")");
                //var books = rep.Find("!(Id in @0)",new long[] { 3,5});
                //var books = rep.FindByPriceOrName(33, "1Learn C");
                //var books = rep.FindFoo(2, "5C1", Order.Asc("Price"));
                //var books = rep.FindFooOrderByPrice(33, "a", new Order[] { Order.Desc("Price"), Order.Asc("PublishDate") });
                //var books = rep.FindByPrice(33);
                //var books = rep.FindByPriceIsNull();
                //var books = rep.FindByPriceAndName(33, "1Learn C");
                //var books = rep.FindByPriceOrNameOrderByPrice(33, "1Learn C");
                //todo:test Paging and single return value
                //todo: support deleteByName,deleteByNameOrAge,DeleteByNameLike
                //var books = rep.FindOrderByPublishDate();
                /*
                var books = rep.FindOrderByPublishDate(new PageRequest {PageNumber=0,PageSize=3,Orders=new Order[] { Order.Asc("Price")} });
                Console.WriteLine(books.PageNumber);
                Console.WriteLine(books.PageSize);
                Console.WriteLine(books.TotalElements);
                Console.WriteLine(books.TotalPages);
                foreach (var b in books.Content)
                */
                //var books = rep.FindByPriceAndNameOrderByPublishDate(33, "2JavaEE Overall");
                var books = rep.FindByPriceOrName(99, "3About Microsoft",Order.Asc("Price"));
                foreach (var b in books)
                {
                    Console.WriteLine(b);
                }
                Console.WriteLine("ok");

                var b1 = rep.FindByName(".net core");
                Console.WriteLine(b1);

                /*
                var books = rep.FindFoo(1, "3About Microsoft", Order.Asc("Priace"));
                foreach(var b in books)
                {
                    Console.WriteLine(b);
                }
                */
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
