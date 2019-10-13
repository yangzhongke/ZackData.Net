using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZackData.NetStandard
{
    public interface ICrudRepository<TEntity, ID> where TEntity:class
    {
        IQueryable<TEntity> Find(Order[] orders, string predicate, params object[] args);

        Page<TEntity> Find(PageRequest pageRequest, string predicate, params object[] args);

        TEntity AddNew(TEntity entity);

        IEnumerable<TEntity> AddNew(IEnumerable<TEntity> entities);

        void DeleteById(ID id);

        void Delete(TEntity entity);

        int DeleteAll(IEnumerable<TEntity> entities);

        TEntity FindById(ID id);

        bool ExistsById(ID id);

        IQueryable<TEntity> FindAll();

        IQueryable<TEntity> FindAll(Order[] orders);

        IQueryable<TEntity> FindAllById(IEnumerable<ID> ids);

        long Count();
    }
}
