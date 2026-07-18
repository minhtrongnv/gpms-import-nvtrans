using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Nvtrans
{
    public class ImportStaffContract
    {
        private readonly SqlDb _db;

        public ImportStaffContract()
        {
            _db = new SqlDb();
        }

        public static string ParseDateToSqlString(string dateValue)
        {
            if (string.IsNullOrWhiteSpace(dateValue))
                return null;

            DateTime date;

            string[] formats =
            {
                "M/d/yyyy h:mm:ss tt",
                "MM/dd/yyyy hh:mm:ss tt",
                "M/d/yyyy h:mm tt",
                "M/d/yyyy"
            };

            bool isValid = DateTime.TryParseExact(
                dateValue.Trim(),
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date
            );

            if (!isValid)
                return null;

            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void InsertOrUpdate(JObject contract)
        {
            string contractId = GetString(contract, "ID");
            string staffId = GetString(contract, "EmployeeID");
            string contractNo = GetString(contract, "ContractNo");
            string startDate = ParseDateToSqlString(GetString(contract, "DateStart"));
            string endDate = ParseDateToSqlString(GetString(contract, "DateEnd"));

            string sql = @"
                IF EXISTS (SELECT 1 FROM dbo.STAFF_CONTRACT_PROCESS WHERE ID = @ContractId)
                    BEGIN
                        UPDATE dbo.STAFF_CONTRACT_PROCESS
                        SET
                            CONTRACT_DESCRIPTION = @ContractNo,
                            START_DATE = @StartDate,
                            END_DATE = @EndDate
                        WHERE ID = @ContractId
                    END
                ELSE
                IF EXISTS (SELECT 1 FROM dbo.STAFF WHERE ID = @StaffId)
                    BEGIN
                        INSERT INTO dbo.STAFF_CONTRACT_PROCESS
                        (
                            ID,
                            STAFF_ID,
                            CONTRACT_DESCRIPTION,
                            START_DATE,
                            END_DATE,
                            DELETED,
                            CREATED_DATE,
                            LAST_UPDATE
                        )
                        VALUES
                        (
                            @ContractId,
                            @StaffId,
                            @ContractNo,
                            @StartDate,
                            @EndDate,
                            0,
                            GETDATE(),
                            GETDATE()
                        )
                    END";
            _db.ExecuteNonQuery(
                sql,
                new SqlParameter("@ContractId", ToDbValue(contractId)),
                new SqlParameter("@StaffId", ToDbValue(staffId)),
                new SqlParameter("@ContractNo", ToDbValue(contractNo)),
                new SqlParameter("@StartDate", ToDbValue(startDate)),
                new SqlParameter("@EndDate", ToDbValue(endDate))
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


    public class ImportStaffContractApiService
    {
        private readonly ApiHelper _apiHelper;
        private readonly string _url;
        private readonly int _pageSize;

        public ImportStaffContractApiService()
        {
            _apiHelper = new ApiHelper();
            _url = "http://nvtrans.lotusshipman.com/D04_Contract/GetList";
            _pageSize = 100;
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
                formData["filter"] = "IsMLC~eq~false";

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
                        ImportStaffContract importer = new ImportStaffContract();
                        importer.InsertOrUpdate(obj);
                    }
                }

                Console.WriteLine("Fetched staff page " + page + ", rows: " + data.Count);

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