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
        IEnumerable<Book> FindFoo(long authorId,string name, Sort sort);

        IEnumerable<Book> FindByPrice(double price);
    }
}
