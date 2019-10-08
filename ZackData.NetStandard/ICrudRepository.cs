using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    public interface ICrudRepository<T, ID>
    {
        T AddNew(T entity);

        IEnumerable<T> AddNew(IEnumerable<T> entities);

        void DeleteById(ID id);

        void Delete(T entity);

        void DeleteAll(IEnumerable<T> entities);

        T FindById(ID id);

        T FindOne(ID id);

        bool ExistsById(ID id);

        IEnumerable<T> FindAll();
        IEnumerable<T> Find(Predicate<T> where, Sort sort=null);
        Page<T> FindAll(PageRequest pageRequest);

        IEnumerable<T> FindAllById(IEnumerable<ID> ids);

        long Count();

        void Save();
    }
}
