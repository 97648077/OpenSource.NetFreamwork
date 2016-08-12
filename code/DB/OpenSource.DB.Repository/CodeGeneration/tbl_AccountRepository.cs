
using System;
using System.Data;
using Dapper;
using OpenSource.DB.IRepository;
using OpenSource.Model;

namespace OpenSource.DB.Repository
{
    public class tbl_AccountRepository : DapperRepository<tbl_Account>, Itbl_AccountRepository
    {
        public int TestTrans(tbl_Account model)
        {
            //事务1:取出数据库连接
            var conn = MyConnection.Pop();
            //事务1:开启事物
            IDbTransaction tran = conn.BeginTransaction();
            //事务1:拼接成SQL
            var sqlParam = SqlGenerator.GetInsert(model);
            //事务1:执行事务1
            conn.Execute(sqlParam.Sql, sqlParam.Param, tran);

            //分表分库so new
            var dapperR = new DapperRepository<tbl_PublicAccountRepository>();
            var conn2 = dapperR.MyConnection.Pop();
            IDbTransaction tran2 = conn2.BeginTransaction();
            //事务2:拼接成SQL
            var sqlParam2 = SqlGenerator.GetInsert(model);
            //事务2:执行事务1
            conn.Execute(sqlParam.Sql, sqlParam2.Param, tran);



            //要么一起提交 ，要么一起回滚
            tran.Commit();
            tran2.Commit();


            //事务1:返回数据库
            MyConnection.Push(conn);
            //事务2:返回数据库
            dapperR.MyConnection.Push(conn);
            return 0;
        }
    }
}
