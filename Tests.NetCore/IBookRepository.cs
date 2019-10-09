using System;
using System.Collections.Generic;
using System.Text;
using YouZack.Entities;
using ZackData.NetStandard;

namespace Tests.NetCore
{
    public interface IBookRepository : ICrudRepository<Book, long>
    {
                
    }
}
