using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    public interface ICrudRepository<TEntity, ID> where TEntity:class
    {
        bool AutoSave { get; set; }
        void Save();

        IEnumerable<TEntity> Find(string predicate, params object[] args);

        IEnumerable<TEntity> Find(Sort sort, string predicate, params object[] args);

        Page<TEntity> Find(PageRequest pageRequest, Sort sort, string predicate, params object[] args);

        IEnumerable<TEntity> FromSQL(string sql, params object[] args);

        int ExecuteSqlCommand(string sql, params object[] args);

        TEntity FindOne(string predicate, params object[] args);
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
        long Count(string predicate, params object[] args);
    }
}
