using System;
using System.Linq;
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
                var repo = new RepositoryGenerator2(() => ctx).Create<Book, long, IBookRepository>();
                //var books1 = repo.FindAll();
                var books1 = repo.FindFoo(3, "3", null);
                //var books1 = repo.FindAllById(new long[] { 1, 2, 5 });
                foreach (var book in books1)
                {
                    Console.WriteLine(book);
                }
            }                
            Console.WriteLine("ok");
            Console.Read();
        }
    }
}
