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
            using (YZDbContext ctx = new YZDbContext("Data Source=.;Initial Catalog=YouZackDB;Integrated Security=False;User ID=sa;Password=abc@123;"))
            {
                var stuGenerator = new RepositoryStubGenerator(() => ctx);
                var repo = stuGenerator.Create<Album,long, IAlbumRepository>();
                //IAlbumRepository repo = (IAlbumRepository)stuGenerator.Create(typeof(Album),typeof(long),typeof(IAlbumRepository));
                //t.hello("yzk", 3);
                /*
                var albums = repo.FindAll();
                foreach (var album in albums)
                {
                    Console.WriteLine(album.Name_En);
                }*/
                /*
                Album a = new Album();
                a.Name_Chs = "a";
                a.Name_En = "b";
                a = repo.AddNew(a);
                Console.WriteLine(a.Id);*/
                PageRequest page = new PageRequest { Offset=0,PageSize=10};
                var albums = repo.FindAll(page);
                foreach (var album in albums.Content)
                {
                    Console.WriteLine(album.Name_En);
                }
            }                
            Console.WriteLine("ok");
            Console.Read();
        }
    }
}
