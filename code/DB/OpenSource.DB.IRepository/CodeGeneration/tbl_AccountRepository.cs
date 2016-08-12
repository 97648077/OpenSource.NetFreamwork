  
using System;
using OpenSource.Model;

namespace OpenSource.DB.IRepository
{
    public interface Itbl_AccountRepository:IDapperRepository<tbl_Account>
    {
        int TestTrans(tbl_Account model);

    }
}
