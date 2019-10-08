using System;
using System.Collections.Generic;
using System.Text;
using YouZack.Entities;
using ZackData.NetStandard;

namespace Tests.NetCore
{
    public interface IAlbumRepository : ICrudRepository<Album,long>
    {
        void hello(string s,int a);
    }
}
