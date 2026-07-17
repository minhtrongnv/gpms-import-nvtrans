using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Nvtrans
{
    public class ImportMasterStaffCert
    {
        private readonly SqlDb _db;

        public ImportMasterStaffCert()
        {
            _db = new SqlDb();
        }

        public void InsertOrUpdate(JObject cert)
        {
            string staffCertId = GetString(cert, "ID");
            string code = GetString(cert, "Code");
            string fullName = GetString(cert, "CertificateTypeName");
            string alertLevel_1 = string.IsNullOrEmpty(GetString(cert, "DayExpireWarning")) ? "0" : GetString(cert, "DayExpireWarning");
            string alertLevel_2 = string.IsNullOrEmpty(GetString(cert, "DayStopWarning")) ? "0": GetString(cert, "DayStopWarning");

            string sql = @"
                IF EXISTS (SELECT 1 FROM dbo.CERTIFICATE WHERE ID = @staffCertId)
                BEGIN
                    UPDATE dbo.CERTIFICATE
                    SET
                        CODE = @Code,
                        NAME = @Name,
                        ALERT_LEVEL1 = @AlertLevel1,
                        ALERT_LEVEL2 = @AlertLevel2,
                        BELONG_TO_SHIP = 0,
                        DELETED = 0,
                        NUMBER = '0',
                        LAST_UPDATE = GETDATE(),
                        CREATED_DATE = GETDATE()
                    WHERE ID = @staffCertId
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.CERTIFICATE
                    (
                        ID,
                        CODE,
                        NAME,
                        ALERT_LEVEL1,
                        ALERT_LEVEL2,
                        BELONG_TO_SHIP,
                        DELETED,
                        NUMBER,
                        LAST_UPDATE,
                        CREATED_DATE
                    )
                    VALUES
                    (
                        @staffCertId,
                        @Code,
                        @Name,
                        @AlertLevel1,
                        @AlertLevel2,
                        0,
                        0,
                        '0',
                        GETDATE(),
                        GETDATE()
                    )
                END";

            _db.ExecuteNonQuery(
                sql,
                new SqlParameter("@staffCertId", ToDbValue(staffCertId)),
                new SqlParameter("@Code", ToDbValue(code)),
                new SqlParameter("@Name", ToDbValue(fullName)),
                new SqlParameter("@AlertLevel1", ToDbValue(alertLevel_1)),
                new SqlParameter("@AlertLevel2", ToDbValue(alertLevel_2))
            );
        }

        private string GetString(JObject obj, params string[] names)
        {
            foreach (string name in names)
            {
                JToken token = obj[name];

                if (token != null && token.Type != JTokenType.Null)
                {
                    return token.ToString().Trim();
                }
            }

            return null;
        }

        private object ToDbValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DBNull.Value;
            }

            return value.Trim();
        }
    }


    public class ImportMasterStaffCertService
    {
        private readonly ApiHelper _apiHelper;
        private readonly string _url;
        private readonly int _pageSize;

        public ImportMasterStaffCertService()
        {
            _apiHelper = new ApiHelper();
            _url = "http://nvtrans.lotusshipman.com/C04_CertificateType/GetList";
            _pageSize = 1000;
        }

        public async Task<List<JObject>> GetAllDataAsync()
        {
            List<JObject> allStaff = new List<JObject>();

            int page = 1;

            while (true)
            {
                Dictionary<string, string> formData = new Dictionary<string, string>();
                formData["page"] = page.ToString();
                formData["pageSize"] = _pageSize.ToString();

                string json = await _apiHelper.PostFormAsync(_url, formData);

                JObject root = JObject.Parse(json);

                JArray data = GetDataArray(root);
                int total = GetInt(root, "Total", "total");

                if (data == null || data.Count == 0)
                {
                    break;
                }

                foreach (JToken item in data)
                {
                    JObject obj = item as JObject;

                    if (obj != null)
                    {
                        allStaff.Add(obj);
                    }
                }

                if (total > 0 && allStaff.Count >= total)
                {
                    break;
                }

                if (data.Count < _pageSize)
                {
                    break;
                }

                page++;
            }

            return allStaff;
        }

        private JArray GetDataArray(JObject root)
        {
            JToken token = root["Data"] ?? root["data"];

            if (token == null)
            {
                return null;
            }

            return token as JArray;
        }

        private int GetInt(JObject obj, params string[] names)
        {
            foreach (string name in names)
            {
                JToken token = obj[name];

                if (token != null)
                {
                    int value;

                    if (int.TryParse(token.ToString(), out value))
                    {
                        return value;
                    }
                }
            }

            return 0;
        }
    }
}