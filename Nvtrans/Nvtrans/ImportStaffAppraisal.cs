using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Nvtrans
{
    public class ImportStaffAppraisal
    {
        private readonly SqlDb _db;
        private static readonly HttpClient _httpClient = CreateHttpClient();
        private static readonly Dictionary<string, string>  appraisalType = new Dictionary<string, string>()
        {
            {
                "KPIType1",
                "9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a001"
            },
            {
                "KPIType2",
                "9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a002"
            },
            {
                "KPIType3",
                "9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a003"
            },
            {
                "KPIType4",
                "9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a004"
            },
            {
                "KPIType5",
                "9e8b8f7a-6d5a-4f9d-8d9f-0d2d59c1a005"
            }
        };

        public ImportStaffAppraisal()
        {
            _db = new SqlDb();
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromMinutes(5);
            return client;
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

        public bool IsStaffAppraisalExisting(string appraisalId)
        {
            if (string.IsNullOrWhiteSpace(appraisalId))
                return false;

            const string sql = @"
                SELECT TOP 1 ID
                FROM dbo.STAFF_EVALUATION
                WHERE ID = @AppraisalId";

            object result = _db.ExecuteScalar(
                sql,
                new SqlParameter("@AppraisalId", appraisalId)
            );

            if (result == null || result == DBNull.Value)
                return false;

            return true;
        }


        public string GetStaffAppraisalTechFileName(string appraisalId)
        {
            if (string.IsNullOrWhiteSpace(appraisalId))
                return null;

            const string sql = @"
                SELECT TOP 1 TECH_FILE
                FROM dbo.STAFF_EVALUATION
                WHERE ID = @AppraisalId";

            object result = _db.ExecuteScalar(
                sql,
                new SqlParameter("@AppraisalId", appraisalId)
            );

            if (result == null || result == DBNull.Value)
                return null;

            return result.ToString();
        }

        public static async Task<bool> DownloadFileAsync(string fileName, string techFileName)
        {
            string baseFilePath = ConfigurationManager.AppSettings["AppraisalAttachDirectory"];
            string domainUrl = ConfigurationManager.AppSettings["DomainUrl"];
            string fileDomainUrl = new Uri(new Uri(domainUrl), "Attachments/").AbsoluteUri;
            string fileUrl = new Uri(new Uri(fileDomainUrl), fileName).AbsoluteUri;

            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("File URL is required.");

            if (!Directory.Exists(baseFilePath))
                Directory.CreateDirectory(baseFilePath);

            using (HttpResponseMessage response = await _httpClient.GetAsync(fileUrl))
            {
                try
                {
                    response.EnsureSuccessStatusCode();

                    byte[] fileBytes =
                        await response.Content.ReadAsByteArrayAsync();

                    string filePath = Path.Combine(baseFilePath, techFileName);

                    File.WriteAllBytes(filePath, fileBytes);

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(fileUrl);
                    return false;
                }
            }
        }

        public void InsertOrUpdate(JObject appraisal)
        {
            string appraisalId = GetString(appraisal, "ID");
            string shipId = "028ED084-BADC-49A4-98E2-248BA50E6557";
            string appraisalTypeId = null;
            appraisalType.TryGetValue(GetString(appraisal, "KPIType"), out appraisalTypeId);
            string staffId = GetString(appraisal, "EmployeeID");
            string attachFile = GetString(appraisal, "FileAttached");
            string appraisalDate = ParseDateToSqlString(GetString(appraisal, "DateKPI"));
            string evaluation = GetString(appraisal, "KPIInfo");
            string evaluator = GetString(appraisal, "Evaluator");


            string sql = @"
                IF EXISTS (SELECT 1 FROM dbo.STAFF_EVALUATION WHERE ID = @AppraisalId)
                    BEGIN
                        UPDATE dbo.STAFF_EVALUATION
                        SET
                            ISSUE_DATE = @AppraisalDate,
                            CONTENT = @Evaluation,
                            REMARK = @Evaluator,
                            EVALUATION_TYPE_ID = @AppraisalTypeId
                        WHERE ID = @AppraisalId
                    END
                ELSE
                IF EXISTS (SELECT 1 FROM dbo.STAFF WHERE ID = @StaffId)
                    BEGIN
                        INSERT INTO dbo.STAFF_EVALUATION
                        (
                            ID,
                            SHIP_ID,
                            STAFF_ID,
                            EVALUATION_TYPE_ID,
                            CONTENT,
                            REMARK,
                            ISSUE_DATE,
                            DELETED,
                            CREATED_DATE,
                            LAST_UPDATE
                        )
                        VALUES
                        (
                            @AppraisalId,
                            @ShipId,
                            @StaffId,
                            @AppraisalTypeId,
                            @Evaluation,
                            @Evaluator,
                            @AppraisalDate,
                            0,
                            GETDATE(),
                            GETDATE()
                        )
                    END";
            _db.ExecuteNonQuery(
                sql,
                new SqlParameter("@AppraisalId", ToDbValue(appraisalId)),
                new SqlParameter("@ShipId", ToDbValue(shipId)),
                new SqlParameter("@AppraisalTypeId", ToDbValue(appraisalTypeId)),
                new SqlParameter("@StaffId", ToDbValue(staffId)),
                new SqlParameter("@AppraisalDate", ToDbValue(appraisalDate)),
                new SqlParameter("@Evaluation", ToDbValue(evaluation)),
                new SqlParameter("@Evaluator", ToDbValue(evaluator))
            );

            if (!string.IsNullOrEmpty(attachFile))
            {
                if (IsStaffAppraisalExisting(appraisalId))
                {
                    string extension = Path.GetExtension(attachFile);
                    string techFileName = GetStaffAppraisalTechFileName(appraisalId);
                    if (string.IsNullOrEmpty(techFileName))
                    {
                        techFileName = Guid.NewGuid().ToString("N") + extension;
                    }
                    else
                    {
                        techFileName = Path.GetFileNameWithoutExtension(techFileName) + extension;
                    }
                    if (!string.IsNullOrEmpty(techFileName))
                    {
                        bool result = DownloadFileAsync(attachFile, techFileName).GetAwaiter().GetResult();
                        if (result)
                        {
                            string fileUpdateSql = @"
                                IF EXISTS (SELECT 1 FROM dbo.STAFF_EVALUATION WHERE ID = @AppraisalId)
                                    BEGIN
                                        UPDATE dbo.STAFF_EVALUATION
                                        SET
                                            TECH_FILE = @TechFileName,
                                            [FILE] = @AttachdFile
                                        WHERE ID = @AppraisalId
                                    END";
                            _db.ExecuteNonQuery(
                               fileUpdateSql,
                               new SqlParameter("@AppraisalId", ToDbValue(appraisalId)),
                               new SqlParameter("@AttachdFile", ToDbValue(attachFile)),
                               new SqlParameter("@TechFileName", ToDbValue(techFileName))
                           );
                        }
                    }
                }
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


    public class ImportStaffAppraisalApiService
    {
        private readonly ApiHelper _apiHelper;
        private readonly string _url;
        private readonly int _pageSize;

        public ImportStaffAppraisalApiService()
        {
            _apiHelper = new ApiHelper();
            _url = "http://nvtrans.lotusshipman.com/D04_KPI/GetList";
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
                        ImportStaffAppraisal importer = new ImportStaffAppraisal();
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