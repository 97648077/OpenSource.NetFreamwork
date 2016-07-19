using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenSource.DB.Repository.DbContext;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using OpenSource.DB.IRepository;
using OpenSource.DB.Repository.SqlGenerator;
using OpenSource.Helps;
using OpenSource.Helps.DB.DbLamda;
using OpenSource.Model;

namespace OpenSource.DB.Repository.Tests
{
    [TestClass]
    public class DapperExTests
    {
        [TestMethod]
        public void FindTests()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void FindPageTest()
        {
            var result = IocManager.IOCManager.Container.GetInstance<Itbl_AccountRepository>();
            var getresult = result.FindAll(x=> x.Id.In(new long[]{1,2,3,4}));
            //DapperRepository<tbl_PublicAccount> _dapper = new DapperRepository<tbl_PublicAccount>();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void UpdateTests()
        {
            var _dapper = IocManager.IOCManager.Container.GetInstance<Itbl_AccountRepository>();
            var result = _dapper.FindAll(x => x.Id == 7 || x.Id == 6).First();



            _dapper.Update(result);
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DeleteTests()
        {
            DapperRepository<tbl_PublicAccount> _dapper = new DapperRepository<tbl_PublicAccount>();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void InsertTests()
        {
            DapperRepository<tbl_PublicAccount> _dapper = new DapperRepository<tbl_PublicAccount>();

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void PagesTests()
        {
            DapperRepository<tbl_PublicAccount> _dapper = new DapperRepository<tbl_PublicAccount>();

            Assert.IsTrue(true);
        }
        [TestMethod]
        public void TransTests()
        {
            var _dapper = IocManager.IOCManager.Container.GetInstance<Itbl_AccountRepository>();
            var result = _dapper.FindAll(x => x.Id == 7 || x.Id == 6).First();
            result.Status = 99;
            _dapper.TestTrans(result);
            Assert.IsTrue(true);
        }
    }
}
