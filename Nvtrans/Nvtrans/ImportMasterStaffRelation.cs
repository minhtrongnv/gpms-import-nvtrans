using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Nvtrans
{
    public class ImportMasterStaffRelation
    {
        private readonly SqlDb _db;

        public ImportMasterStaffRelation()
        {
            _db = new SqlDb();
        }

        public void InsertOrUpdate()
        {
            string sql = @"
                INSERT INTO STAFF_RELATED
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
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a001', 'HouseHold', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a002', 'Wife', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a003', 'Husband', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a004', 'Father', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a005', 'Mother', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a006', 'YoungBrother', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a007', 'OldBrother', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a008', 'Sister', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a009', 'Child', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a010', 'GrandChild', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a011', 'GrandFather', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a012', 'GrandMother', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a013', 'FatherAunt', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a014', 'MotherAunt', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a015', 'FatherYoungUncle', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a016', 'FatherYoungAunt', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a017', 'FatherOldUncle', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a018', 'MotherUncle', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a019', 'Auntie', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a020', 'DaughterInLaw', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a021', 'SonInLaw', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a022', 'YoungChild', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350'),
                    ('9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a023', 'Others', 0, '2026-07-07T14:58:55.350', '2026-07-07T14:58:55.350')
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
                    FROM STAFF_RELATED AS existing
                    WHERE existing.ID = source.ID
                );
            ";

            _db.ExecuteNonQuery(
                sql
            );
        }
    }
}