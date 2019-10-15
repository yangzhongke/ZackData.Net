using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YouZack.Entities;
using ZackData.NetStandard;

namespace Tests.NetCore
{
    public interface IBookRepository : ICrudRepository<Book, long>
    {
        [Predicate("AuthorId=@0 or Name=@1")]
        IQueryable<Book> FindFoo(long authorId,string name, Order order);

        [Predicate("AuthorId=@0 or Name=@1")]
        IQueryable<Book> FindFooOrderByPrice(long authorId, string name, Order[] sorts);

        IQueryable<Book> FindByPrice(double price);
        IQueryable<Book> FindByPriceIsNull();

        IQueryable<Book> FindByPriceAndName(double price,string name);

        IQueryable<Book> FindByPriceOrName(double price, string name);

        IQueryable<Book> FindByPriceOrNameOrderByPrice(double price, string name);

        IQueryable<Book> FindByName(string name);
        IQueryable<Book> FindByAgeOrderByPrice(int age);
        IQueryable<Book> FindByAgeOrderByPriceDesc(int age);

        IQueryable<Book> FindOrderByPublishDate();

        
    }
}
