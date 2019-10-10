using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    public interface ICrudRepository<TEntity, ID> where TEntity:class
    {
        bool AutoSave { get; set; }
        void Save();

        TEntity AddNew(TEntity entity);

        IEnumerable<TEntity> AddNew(IEnumerable<TEntity> entities);

        void DeleteById(ID id);

        void Delete(TEntity entity);

        void DeleteAll(IEnumerable<TEntity> entities);

        TEntity FindById(ID id);

        bool ExistsById(ID id);

        IEnumerable<TEntity> FindAll();

        IEnumerable<TEntity> FindAll(Sort sort);
        //Page<T> FindAll(PageRequest pageRequest);

        IEnumerable<TEntity> FindAllById(IEnumerable<ID> ids);

        long Count();
    }
}
