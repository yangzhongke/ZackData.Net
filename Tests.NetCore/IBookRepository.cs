using System;
using System.Collections.Generic;
using System.Text;
using YouZack.Entities;
using ZackData.NetStandard;

namespace Tests.NetCore
{
    public interface IBookRepository : ICrudRepository<Book, long>
    {
        [Predicate("AuthorId=@0 or Name=@1")]
        IEnumerable<Book> FindFoo(long authorId,string name, Order order);

        [Predicate("AuthorId=@0 or Name=@1")]
        IEnumerable<Book> FindFooOrderByPrice(long authorId, string name, Order[] sorts);

        IEnumerable<Book> FindByPrice(double price);
        IEnumerable<Book> FindByPriceIsNull();

        IEnumerable<Book> FindByPriceAndName(double price,string name);

        IEnumerable<Book> FindByPriceOrName(double price, string name);

        IEnumerable<Book> FindByPriceOrNameOrderByPrice(double price, string name);

        IEnumerable<Book> FindByName(string name);
        IEnumerable<Book> FindByAgeOrderByPrice(int age);
        IEnumerable<Book> FindByAgeOrderByPriceDesc(int age);
    }
}
