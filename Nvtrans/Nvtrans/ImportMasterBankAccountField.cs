using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Nvtrans
{
    public class ImportMasterBankAccountField
    {
        private readonly SqlDb _db;

        public ImportMasterBankAccountField()
        {
            _db = new SqlDb();
        }

        public void InsertOrUpdate()
        {
            string sql = @"
                INSERT INTO STAFF_FIELDTEMPLATE
                (
                    ID,
                    FIELD,
                    DELETED,
                    LAST_UPDATE,
                    CREATED_DATE
                )
                SELECT
                    source.ID,
                    source.FIELD,
                    source.DELETED,
                    source.LAST_UPDATE,
                    source.CREATED_DATE
                FROM
                (
                    VALUES
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a001', 'AccountName', 0, GETDATE(), GETDATE()),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a003', 'AccountNumber', 0, GETDATE(), GETDATE()),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a002', 'Bank', 0, GETDATE(), GETDATE()),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a023', 'BankBranch', 0, GETDATE(), GETDATE())
                ) AS source
                (
                    ID,
                    FIELD,
                    DELETED,
                    LAST_UPDATE,
                    CREATED_DATE
                )
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM STAFF_FIELDTEMPLATE AS existing
                    WHERE existing.ID = source.ID
                );
            ";

            _db.ExecuteNonQuery(
                sql
            );
        }
    }
}