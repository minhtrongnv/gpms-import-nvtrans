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
    public class PartialDateResult
    {
        public string DayMonth { get; set; }
        public string Year { get; set; } 
    }


    public class ImportStaffRelated
    {
        private readonly SqlDb _db;

        public ImportStaffRelated()
        {
            _db = new SqlDb();
        }

        public static PartialDateResult ParserRelativeDOB(string value)
        {
            var result = new PartialDateResult();

            if (string.IsNullOrWhiteSpace(value) ||
                value.Trim().Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            value = value.Trim();

            Match match = Regex.Match(
                value,
                @"^(?<day>\d{2}|__)/(?<month>\d{2}|__)/(?<year>\d{4})$");

            if (!match.Success)
            {

            }

            string day = match.Groups["day"].Value;
            string month = match.Groups["month"].Value;
            string year = match.Groups["year"].Value;


            result.DayMonth = day + "/" + month;
            result.Year = year;

            return result;
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

        public string GetRelationId(string relationName)
        {
            if (string.IsNullOrWhiteSpace(relationName))
                return null;

            const string sql = @"
                SELECT TOP 1 ID
                FROM dbo.STAFF_RELATED
                WHERE LTRIM(RTRIM(NAME)) =
              LTRIM(RTRIM(@RelationName))";

            object result = _db.ExecuteScalar(
                sql,
                new SqlParameter("@RelationName", relationName.Trim())
            );

            if (result == null || result == DBNull.Value)
                return null;

            return result.ToString();
        }


        public void InsertOrUpdate(JObject relative)
        {
            string relativeId = GetString(relative, "ID");
            string staffId = GetString(relative, "EmployeeID");
            string relativeName = GetString(relative, "RelativeName");
            string relativeTypeId = GetRelationId(GetString(relative, "RelationType"));
            var relativeDOB = ParserRelativeDOB(GetString(relative, "DOB"));
            string sql = @"
                IF EXISTS (SELECT 1 FROM dbo.STAFF_FAMILY_RELATIONSHIP WHERE ID = @RelativeId)
                    BEGIN
                        UPDATE dbo.STAFF_FAMILY_RELATIONSHIP
                        SET
                            FULLNAME = @RelativeName,
                            DATE_OF_BIRTH = @DayMonth,
                            YEAR_OF_BIRTH = @Year,
                            STAFF_RELATED_ID = @RelativeTypeId
                        WHERE ID = @RelativeId
                    END
                ELSE
                IF EXISTS (SELECT 1 FROM dbo.STAFF WHERE ID = @StaffId)
                    BEGIN
                        INSERT INTO dbo.STAFF_FAMILY_RELATIONSHIP
                        (
                            ID,
                            FULLNAME,
                            DATE_OF_BIRTH,
                            YEAR_OF_BIRTH,
                            STAFF_RELATED_ID,
                            STAFF_ID,
                            DELETED,
                            CREATED_DATE,
                            LAST_UPDATE
                        )
                        VALUES
                        (
                            @RelativeId,
                            @RelativeName,
                            @DayMonth,
                            @Year,
                            @RelativeTypeId,
                            @StaffId,
                            0,
                            GETDATE(),
                            GETDATE()
                        )
                    END";
            _db.ExecuteNonQuery(
                sql,
                new SqlParameter("@RelativeId", ToDbValue(relativeId)),
                new SqlParameter("@StaffId", ToDbValue(staffId)),
                new SqlParameter("@RelativeName", ToDbValue(relativeName)),
                new SqlParameter("@RelativeTypeId", ToDbValue(relativeTypeId)),
                new SqlParameter("@DayMonth", ToDbValue(relativeDOB.DayMonth)),
                new SqlParameter("@Year", ToDbValue(relativeDOB.Year))
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


    public class ImportStaffRelatedApiService
    {
        private readonly ApiHelper _apiHelper;
        private readonly string _url;
        private readonly int _pageSize;

        public ImportStaffRelatedApiService()
        {
            _apiHelper = new ApiHelper();
            _url = "http://nvtrans.lotusshipman.com/D04_Relative/GetList";
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
                        ImportStaffRelated importer = new ImportStaffRelated();
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