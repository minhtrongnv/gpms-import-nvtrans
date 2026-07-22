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
    public class ImportStaff
    {
        private readonly SqlDb _db;
        private readonly bool _isTerminated;

        public ImportStaff(bool isTerminated = false)
        {
            _db = new SqlDb();
            _isTerminated = isTerminated;
        }

        public static void SplitVietnameseName(
            string fullName,
            out string firstName,
            out string lastName)
        {
            firstName = null;
            lastName = null;

            if (string.IsNullOrWhiteSpace(fullName))
                return;

            string[] parts = fullName
                .Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            firstName = parts[parts.Length - 1];

            if (parts.Length > 1)
            {
                lastName = string.Join(" ", parts.Take(parts.Length - 1));
            }
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


        public void InsertOrUpdate(JObject staff)
        {
            string staffId = GetString(staff, "ID");
            string code = GetString(staff, "Code");
            SplitVietnameseName(
                GetString(staff, "EmployeeName"),
                out string firstName,
                out string lastName
            );
            string dob = ParseDateToSqlString(GetString(staff, "DOB"));
            string contractDate= ParseDateToSqlString(GetString(staff, "DateEmployment"));
            string IDNumber = GetString(staff, "IDNumber");
            string email = GetString(staff, "PrivateEmail", "BusinessEmail");
            string phone = GetString(staff, "MobilePhone");
            string homePhone = GetString(staff,  "HomePhone");
            string PermanentAddress = GetString(staff, "PermanentAddress");
            string TemporaryAddress = GetString(staff, "TemporaryAddress");
            string status = "Ashore";
            string graduationId = GetString(staff, "GraduationID");
            int married = GetString(staff, "MaritalStatus") == "Married" ? 1 : 0;
            int gender = GetString(staff, "Gender") == "Male" ? 0 : 1;
            string graduationYear = GetString(staff, "GraduationYear");
            string EmergencyContactName = GetString(staff, "EmergencyContactName");
            string EmergencyContactPhone = GetString(staff, "EmergencyContactPhone");
            string EmergencyContactAdress = GetString(staff, "EmergencyContactAdress");
            string EmergencyRelationId = GetRelationId(GetString(staff, "EmergencyRelationType"));
            string Note = GetString(staff, "Description");
            string sleepleavedId = null;
            if (_isTerminated)
            {
                status = "Sleep/Leaved";
            }
            string sql = @"
                IF EXISTS (SELECT 1 FROM dbo.STAFF WHERE ID = @StaffId)
                BEGIN
                    UPDATE dbo.STAFF
                    SET
                        CODE = @Code,
                        EMAIL = @Email,
                        PHONE = @Phone,
                        FIRST_NAME = @FirstName,
                        LAST_NAME = @LastName,
                        BIRTH_DAY = @Dob,
                        GENDER = @Gender,
                        ADDRESS = @TemporaryAddress,
                        PERMANENT_ADDRESS = @PermanentAddress,
                        CPERSON = @CPerson,
                        CPHONE = @CPhone,
                        CADDRESS = @CAddress,
                        STATUS = @Status,
                        COMPANY_ID = @CompanyId,
                        LABOR_CONTRACT_DATE = @ContractDate,
                        IDENTITY_NUMBER = @IDNumber,
                        NOTE = @Note,
                        RELATED_ID = @EmergencyRelationId,
                        EDUCATION_QUALIFICATIONS_ID = @GraduationId,
                        GRADUATION_YEAR = @GraduationYear,
                        MARRIED = @Married,
                        HOME_PHONE = @HomePhone,
                        DELETED = 0
                    WHERE ID = @StaffId
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.STAFF
                    (
                        ID,
                        CODE,
                        EMAIL,
                        PHONE,
                        HOME_PHONE,
                        FIRST_NAME,
                        LAST_NAME,
                        BIRTH_DAY,
                        GENDER,
                        ADDRESS,
                        PERMANENT_ADDRESS,
                        CPERSON,
                        CPHONE,
                        CADDRESS,
                        STATUS,
                        COMPANY_ID,
                        LABOR_CONTRACT_DATE,
                        IDENTITY_NUMBER,
                        NOTE,
                        RELATED_ID,
                        EDUCATION_QUALIFICATIONS_ID,
                        GRADUATION_YEAR,
                        MARRIED,
                        LAST_UPDATE,
                        CREATED_DATE,
                        ACTIVE,
                        DELETED
                    )
                    VALUES
                    (
                        @StaffId,
                        @Code,
                        @Email,
                        @Phone,
                        @HomePhone,
                        @FirstName,
                        @LastName,
                        @Dob,
                        @Gender,
                        @TemporaryAddress,
                        @PermanentAddress,
                        @CPerson,
                        @CPhone,
                        @CAddress,
                        @Status,
                        @CompanyId,
                        @ContractDate,
                        @IDNumber,
                        @Note,
                        @EmergencyRelationId,
                        @GraduationId,
                        @GraduationYear,
                        @Married,
                        GETDATE(),
                        GETDATE(),
                        1,
                        0
                    )
                END";

            _db.ExecuteNonQuery(
                sql,
                new SqlParameter("@StaffId", ToDbValue(staffId)),
                new SqlParameter("@Code", ToDbValue(code)),
                new SqlParameter("@Email", ToDbValue(email)),
                new SqlParameter("@Phone", ToDbValue(phone)),
                new SqlParameter("@LastName", ToDbValue(lastName)),
                new SqlParameter("@FirstName", ToDbValue(firstName)),
                new SqlParameter("@Dob", ToDbValue(dob)),
                new SqlParameter("@Gender", ToDbValue(gender.ToString())),
                new SqlParameter("@TemporaryAddress", ToDbValue(TemporaryAddress)),
                new SqlParameter("@PermanentAddress", ToDbValue(PermanentAddress)),
                new SqlParameter("@CPerson", ToDbValue(EmergencyContactName)),
                new SqlParameter("@CPhone", ToDbValue(EmergencyContactPhone)),
                new SqlParameter("@CAddress", ToDbValue(EmergencyContactAdress)),
                new SqlParameter("@Status", ToDbValue(status)),
                new SqlParameter("@CompanyId", ToDbValue("A6F8FBCE-1F0F-4B38-B523-B77983129FF5")),
                new SqlParameter("@ContractDate", ToDbValue(contractDate)),
                new SqlParameter("@IDNumber", ToDbValue(IDNumber)),
                new SqlParameter("@Note", ToDbValue(Note)),
                new SqlParameter("@EmergencyRelationId", ToDbValue(EmergencyRelationId)),
                new SqlParameter("@GraduationId", ToDbValue(graduationId)), 
                new SqlParameter("@GraduationYear", ToDbValue(graduationYear)),
                new SqlParameter("@Married", ToDbValue(married.ToString())),
                new SqlParameter("@HomePhone", ToDbValue(homePhone)),
                new SqlParameter("@SleepLeavedId", ToDbValue(sleepleavedId))
            );

            //Import MoreInfo
            var listMoreInfoField = new Dictionary<string, string> { 
                { "AccountName",  "9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a001" },
                { "AccountNo", "9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a003" },
                { "BankName", "9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a002" },
                { "BranchName", "9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a023" }
            };
            foreach (var entry in listMoreInfoField)
            {
                string moreInfoValue = !string.IsNullOrEmpty(GetString(staff, entry.Key)) ? GetString(staff, entry.Key) : "";
                string moreInfoSql = @"
                    IF EXISTS (SELECT 1 FROM dbo.STAFF_MOREINFORMATION WHERE STAFF_FIELD_ID = @FieldId and STAFF_ID = @StaffId )
                        BEGIN
                            UPDATE dbo.STAFF_MOREINFORMATION
                            SET
                                VALUE = @FieldValue,
                                DELETED = 0,
                                LAST_UPDATE = GETDATE(),
                                CREATED_DATE = GETDATE()
                            WHERE STAFF_FIELD_ID = @FieldId and STAFF_ID = @StaffId
                        END
                    ELSE
                        BEGIN
                            INSERT INTO dbo.STAFF_MOREINFORMATION
                            (
                                ID,
                                STAFF_ID,
                                STAFF_FIELD_ID,
                                VALUE,
                                DELETED,
                                LAST_UPDATE,
                                CREATED_DATE,
                                CREW_WORK_ID
                            )
                            VALUES
                            (
                                NEWID(),
                                @StaffId,
                                @FieldId,
                                @FieldValue,
                                0,
                                GETDATE(),
                                GETDATE(),
                                0
                            )
                        END";

                _db.ExecuteNonQuery(
                    moreInfoSql,
                    new SqlParameter("@FieldId", ToDbValue(entry.Value)),
                    new SqlParameter("@FieldValue", ToDbValue(moreInfoValue)),
                    new SqlParameter("@StaffId", ToDbValue(staffId))
                );
            }
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


    public class ImportStaffApiService
    {
        private readonly ApiHelper _apiHelper;
        private readonly string _url;
        private readonly int _pageSize;

        public ImportStaffApiService(bool isTerminated)
        {
            _apiHelper = new ApiHelper();
            _url = isTerminated ? "http://nvtrans.lotusshipman.com/D04_TerminationReport/GetList" : "http://nvtrans.lotusshipman.com/D04_Employee/GetList";
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
                        allStaff.Add(obj);
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