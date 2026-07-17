using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Nvtrans
{
    public class ImportMasterPosition
    {
        private readonly SqlDb _db;

        public ImportMasterPosition()
        {
            _db = new SqlDb();
        }

        public void ImportFromFile(string filePath)
        {
            string content = File.ReadAllText(filePath);
            JArray positions = JArray.Parse(content);
            int importedCount = 0;

            foreach (JToken token in positions)
            {
                JObject position = token as JObject;
                if (InsertOrUpdate(position))
                {
                    importedCount++;
                }
            }
        }

        public bool InsertOrUpdate(JObject position)
        {
            string positionId = GetString(position, "ID");
            string mappingId = GetString(position, "MappingId");
            string code = GetString(position, "Code");
            string positionName = GetString(position, "PositionName");
            bool isNew = GetBool(position, "IsNew");
            if (isNew)
            {
                positionId = mappingId;
                const string sql = @"
                    IF EXISTS
                    (
                        SELECT 1
                        FROM dbo.POSITION
                        WHERE ID = @SourceId
                    )
                    BEGIN
                        UPDATE dbo.POSITION
                        SET
                            CODE = @Code,
                            NAME = @PositionName,
                            CREATED_DATE = GETDATE(),
                            LAST_UPDATE = GETDATE()
                        WHERE ID = @SourceId
                    END
                    ELSE
                    BEGIN
                        INSERT INTO dbo.POSITION
                        (
                            ID,
                            CODE,
                            NAME,
                            COMPANY_ID,
                            IS_ADMIN,
                            DELETED,
                            ORIGINAL,
                            IS_OFFICER,
                            CREATED_DATE,
                            LAST_UPDATE
                        )
                        VALUES
                        (
                            @SourceId,
                            @Code,
                            @PositionName,
                            'A6F8FBCE-1F0F-4B38-B523-B77983129FF5',
                            0,
                            0,
                            0,
                            0,
                            GETDATE(),
                            GETDATE()
                        )
                    END";

                _db.ExecuteNonQuery(
                    sql,
                    new SqlParameter("@SourceId", positionId),
                    new SqlParameter("@Code", code),
                    new SqlParameter("@PositionName", positionName)
                );
            }

            return true;
        }

        private string GetString(JObject obj, params string[] names)
        {
            foreach (string name in names)
            {
                JToken token = obj[name];

                if (token != null && token.Type != JTokenType.Null)
                {
                    string value = token.ToString().Trim();
                    return value.Length == 0 ? null : value;
                }
            }

            return null;
        }

        private bool GetBool(JObject obj, params string[] names)
        {
            foreach (string name in names)
            {
                JToken token = obj[name];

                if (token == null || token.Type == JTokenType.Null)
                {
                    continue;
                }

                bool value;

                if (bool.TryParse(token.ToString(), out value))
                {
                    return value;
                }

                int number;

                if (int.TryParse(token.ToString(), out number))
                {
                    return number != 0;
                }
            }

            return false;
        }
    }
}