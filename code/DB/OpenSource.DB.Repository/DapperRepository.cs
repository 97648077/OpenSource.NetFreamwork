#if COREFX
using IDbConnection = System.Data.Common.DbConnection;
#endif

using Dapper;
using OpenSource.DB.Repository.Cache;
using OpenSource.DB.Repository.DbContext;
using OpenSource.DB.Repository.SqlGenerator;
using OpenSource.Helps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using OpenSource.DB.IRepository;

namespace OpenSource.DB.Repository
{
    abstract public class DapperRepository<TEntity> : IDapperRepository<TEntity> where TEntity : class
    {
        public DapperRepository()
        {
            MyConnection = new DapperDbContextDir(typeof(TEntity)).dbConnPool;
            SqlGenerator = SqlGeneratorDir.ExistModelDesCache<TEntity>(ESqlConnector.MSSQL);
        }

        public DapperDbContext MyConnection { get; }

        public ISqlGenerator<TEntity> SqlGenerator { get; }

        #region Find

        public virtual TEntity Find(Expression<Func<TEntity, bool>> expression)
        {
            var queryResult = SqlGenerator.GetSelectFirst(expression);
            return FindAll(queryResult).FirstOrDefault();
        }

        public virtual IEnumerable<TEntity> FindAll()
        {
            var queryResult = SqlGenerator.GetSelectAll(null);
            return FindAll(queryResult);
        }

        public virtual IEnumerable<TEntity> FindAll(Expression<Func<TEntity, bool>> expression)
        {
            var queryResult = SqlGenerator.GetSelectAll(expression);
            var Connection = MyConnection.Pop();
            var result = Connection.Query<TEntity>(queryResult.Sql, queryResult.Param);
            MyConnection.Push(Connection);
            return result;
        }

        public virtual IEnumerable<TEntity> FindAll(SqlQuery sqlQuery)
        {
            var Connection = MyConnection.Pop();
            var result = Connection.Query<TEntity>(sqlQuery.Sql, sqlQuery.Param);
            MyConnection.Push(Connection);
            return result;
        }

        #endregion Find

        #region Insert

        public virtual bool Insert(TEntity instance)
        {
            var Connection = MyConnection.Pop();
            bool added;

            var queryResult = SqlGenerator.GetInsert(instance);

            if (SqlGenerator.IsIdentity)
            {
                var newId = Connection.Query<long>(queryResult.Sql, queryResult.Param).FirstOrDefault();
                added = newId > 0;

                if (added)
                {
                    var newParsedId = Convert.ChangeType(newId, SqlGenerator.IdentityProperty.PropertyInfo.PropertyType);
                    SqlGenerator.IdentityProperty.PropertyInfo.SetValue(instance, newParsedId);
                }
            }
            else
            {
                added = Connection.Execute(queryResult.Sql, instance) > 0;
            }
            MyConnection.Push(Connection);
            return added;
        }

        #endregion Insert

        #region Delete
        public virtual bool Delete(TEntity instance)
        {
            var queryResult = SqlGenerator.GetDelete(instance);
            return Delete(queryResult);
        }

        public virtual bool Delete(Expression<Func<TEntity, bool>> expression)
        {
            var queryResult = SqlGenerator.GetDelete(expression);
            return Delete(queryResult);
        }

        public virtual bool Delete(SqlQuery queryResult)
        {
            var Connection = MyConnection.Pop();
            var deleted = Connection.Execute(queryResult.Sql, queryResult.Param) > 0;
            MyConnection.Push(Connection);
            return deleted;
        }

        #endregion Delete

        #region Update
        public virtual bool Update(TEntity instance)
        {
            var query = SqlGenerator.GetUpdate(instance);
            return Update(query);
        }

        public virtual bool Update(TEntity instance, Expression<Func<TEntity, object>> field)
        {
            var query = SqlGenerator.GetUpdate(instance, field);
            return Update(query);
        }

        public virtual bool Update(TEntity instance, Expression<Func<TEntity, object>> field, Expression<Func<TEntity, bool>> expression)
        {
            var query = SqlGenerator.GetUpdate(instance, field, expression);
            return Update(query);
        }

        private bool Update(SqlQuery query)
        {
            var Connection = MyConnection.Pop();
            var updated = Connection.Execute(query.Sql, query.Param) > 0;
            MyConnection.Push(Connection);
            return updated;
        }
        #endregion Update

        #region Beetwen
        public IEnumerable<TEntity> FindAllBetween(object from, object to, Expression<Func<TEntity, object>> btwField)
        {
            return FindAllBetween(from, to, btwField, null);
        }

        public IEnumerable<TEntity> FindAllBetween(object from, object to, Expression<Func<TEntity, object>> btwField, Expression<Func<TEntity, bool>> expression)
        {
            var queryResult = SqlGenerator.GetSelectBetween(from, to, btwField, expression);
            var Connection = MyConnection.Pop();
            var data = Connection.Query<TEntity>(queryResult.Sql, queryResult.Param);
            MyConnection.Push(Connection);
            return data;
        }


        #endregion Beetwen

        #region Pages

        public PageListView<TEntity> FindAllPages(long from, long to, Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> field, bool isDesc = false)
        {

            var queryResult = SqlGenerator.GetSelectAll(expression);
            var countResult = SqlGenerator.GetSelectCount(queryResult.Sql, queryResult.Param);
            var pageResult = SqlGenerator.GetSelectPages(from, to, queryResult.Sql, queryResult.Param, field, isDesc);
            var Connection = MyConnection.Pop();
            var result = new PageListView<TEntity>
            {
                PageIndex = from,
                PageSize = to,
                DataRows = Convert.ToInt64(Connection.ExecuteScalar(countResult.Sql, countResult.Param)),
                Data = Connection.Query<TEntity>(pageResult.Sql, pageResult.Param) as List<TEntity>
            };
            MyConnection.Push(Connection);
            return result;
        }

        #endregion
    }
}