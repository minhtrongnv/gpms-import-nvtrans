using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Nvtrans
{
    public class ImportMasterEducation
    {
        private readonly SqlDb _db;

        public ImportMasterEducation()
        {
            _db = new SqlDb();
        }

        public void InsertOrUpdate(JObject staff)
        {
            string staffId = GetString(staff, "ID", "Id", "EmployeeId", "EMPLOYEE_ID", "StaffId");
            string code = GetString(staff, "Code", "EmployeeCode", "EMPLOYEE_CODE");
            string fullName = GetString(staff, "FullName", "Name", "FULL_NAME", "EMPLOYEE_NAME");
            string departmentName = GetString(staff, "DepartmentName", "Department", "DEPARTMENT_NAME");
            string positionName = GetString(staff, "PositionName", "Position", "POSITION_NAME");
            string email = GetString(staff, "Email", "EMAIL");
            string phone = GetString(staff, "Phone", "Tel", "Mobile", "PHONE");

            if (string.IsNullOrWhiteSpace(staffId))
            {
                staffId = code;
            }

            if (string.IsNullOrWhiteSpace(staffId))
            {
                Console.WriteLine("Skip staff because StaffId and Code are empty.");
                return;
            }

            string sql = @"
                IF EXISTS (SELECT 1 FROM dbo.ImportStaff WHERE StaffId = @StaffId)
                BEGIN
                    UPDATE dbo.ImportStaff
                    SET
                        Code = @Code,
                        FullName = @FullName,
                        DepartmentName = @DepartmentName,
                        PositionName = @PositionName,
                        Email = @Email,
                        Phone = @Phone,
                        UpdatedAt = GETDATE()
                    WHERE StaffId = @StaffId
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.ImportStaff
                    (
                        StaffId,
                        Code,
                        FullName,
                        DepartmentName,
                        PositionName,
                        Email,
                        Phone,
                        CreatedAt,
                        UpdatedAt
                    )
                    VALUES
                    (
                        @StaffId,
                        @Code,
                        @FullName,
                        @DepartmentName,
                        @PositionName,
                        @Email,
                        @Phone,
                        GETDATE(),
                        GETDATE()
                    )
                END";

            _db.ExecuteNonQuery(
                sql,
                new SqlParameter("@StaffId", ToDbValue(staffId)),
                new SqlParameter("@Code", ToDbValue(code)),
                new SqlParameter("@FullName", ToDbValue(fullName)),
                new SqlParameter("@DepartmentName", ToDbValue(departmentName)),
                new SqlParameter("@PositionName", ToDbValue(positionName)),
                new SqlParameter("@Email", ToDbValue(email)),
                new SqlParameter("@Phone", ToDbValue(phone))
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


    public class ImportMasterEducationApiService
    {
        private readonly ApiHelper _apiHelper;
        private readonly string _url;
        private readonly int _pageSize;

        public ImportMasterEducationApiService()
        {
            _apiHelper = new ApiHelper();
            _url = "http://nvtrans.lotusshipman.com/C03_Education/GetList";
            _pageSize = 20;
        }

        public async Task<List<JObject>> GetAllDataAsync()
        {
            List<JObject> allStaff = new List<JObject>();

            int page = 1;

            while (true)
            {
                Dictionary<string, string> formData = new Dictionary<string, string>();
                formData["sort"] = "Code-asc";
                formData["page"] = page.ToString();
                formData["pageSize"] = _pageSize.ToString();
                formData["group"] = "";
                formData["filter"] = "";

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