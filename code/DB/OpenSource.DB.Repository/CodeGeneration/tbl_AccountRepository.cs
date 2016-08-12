
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
            //����1:ȡ�����ݿ�����
            var conn = MyConnection.Pop();
            //����1:��������
            IDbTransaction tran = conn.BeginTransaction();
            //����1:ƴ�ӳ�SQL
            var sqlParam = SqlGenerator.GetInsert(model);
            //����1:ִ������1
            conn.Execute(sqlParam.Sql, sqlParam.Param, tran);

            //�ֱ�ֿ�so new
            var dapperR = new DapperRepository<tbl_PublicAccountRepository>();
            var conn2 = dapperR.MyConnection.Pop();
            IDbTransaction tran2 = conn2.BeginTransaction();
            //����2:ƴ�ӳ�SQL
            var sqlParam2 = SqlGenerator.GetInsert(model);
            //����2:ִ������1
            conn.Execute(sqlParam.Sql, sqlParam2.Param, tran);



            //Ҫôһ���ύ ��Ҫôһ��ع�
            tran.Commit();
            tran2.Commit();


            //����1:�������ݿ�
            MyConnection.Push(conn);
            //����2:�������ݿ�
            dapperR.MyConnection.Push(conn);
            return 0;
        }
    }
}
