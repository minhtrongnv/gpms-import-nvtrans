using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Nvtrans
{
    public class ImportMasterStaffAppraisalType
    {
        private readonly SqlDb _db;

        public ImportMasterStaffAppraisalType()
        {
            _db = new SqlDb();
        }

        public void InsertOrUpdate()
        {
            string sql = @"
                INSERT INTO EVALUTION_TYPE
                (
                    ID,
                    NAME,
                    DELETED,
                    LAST_UPDATE,
                    CREATED_DATE
                )
                SELECT
                    source.ID,
                    source.NAME,
                    source.DELETED,
                    source.LAST_UPDATE,
                    source.CREATED_DATE
                FROM
                (
                    VALUES
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a001', N'Đánh giá định kỳ', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a002', N'Đánh giá phỏng vấn đầu vào', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a003', N'Đánh giá phỏng vấn nâng chức danh', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a004', N'Đánh giá sau đào tạo', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a005', N'Đánh giá tổng thể nguồn lực', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350')
                ) AS source
                (
                    ID,
                    NAME,
                    DELETED,
                    LAST_UPDATE,
                    CREATED_DATE
                )
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM EVALUTION_TYPE AS existing
                    WHERE existing.ID = source.ID
                );
            ";

            _db.ExecuteNonQuery(
                sql
            );
        }
    }
}