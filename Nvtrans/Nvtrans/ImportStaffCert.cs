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
    public class ImportStaffCert
    {
        private readonly SqlDb _db;
        private static readonly HttpClient _httpClient = CreateHttpClient();

        public ImportStaffCert()
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

        public bool IsStaffCertExisting(string staffCertId)
        {
            if (string.IsNullOrWhiteSpace(staffCertId))
                return false;

            const string sql = @"
                SELECT TOP 1 ID
                FROM dbo.STAFF_CERTIFICATE_REL
                WHERE ID = @StaffCertId";

            object result = _db.ExecuteScalar(
                sql,
                new SqlParameter("@StaffCertId", staffCertId)
            );

            if (result == null || result == DBNull.Value)
                return false;

            return true;
        }


        public string GetStaffTechFileName(string staffCertId)
        {
            if (string.IsNullOrWhiteSpace(staffCertId))
                return null;

            const string sql = @"
                SELECT TOP 1 TECH_ATTACHED_FILE
                FROM dbo.STAFF_CERTIFICATE_REL
                WHERE ID = @StaffCertId";

            object result = _db.ExecuteScalar(
                sql,
                new SqlParameter("@StaffCertId", staffCertId)
            );

            if (result == null || result == DBNull.Value)
                return null;

            return result.ToString();
        }

        public static async Task<bool> DownloadFileAsync(string fileName, string techFileName)
        {
            string baseFilePath = ConfigurationManager.AppSettings["StaffCertDirectory"];
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
                catch(Exception ex)
                {
                    Console.WriteLine(fileUrl);
                    return false;
                }
            }
        }

        public void InsertOrUpdate(JObject cert)
        {
            string staffCertId = GetString(cert, "ID");
            string certId = GetString(cert, "CertificateTypeID");
            string certCode = GetString(cert, "No");
            string issueDate = ParseDateToSqlString(GetString(cert, "DateofIssue"));
            string expiredDate = ParseDateToSqlString(GetString(cert, "DateExpired"));
            string description = GetString(cert, "Description");
            string staffId = GetString(cert, "EmployeeID");
            string attachFile = GetString(cert, "AttachFile");

            string sql = @"
                IF EXISTS (SELECT 1 FROM dbo.STAFF_CERTIFICATE_REL WHERE ID = @StaffCertId)
                    BEGIN
                        UPDATE dbo.STAFF_CERTIFICATE_REL
                        SET
                            CERT_ID = @CertId,
                            STAFF_ID = @StaffId,
                            DATE_OF_ISSUE = @IssueDate,
                            DATE_EXPIRATION = @ExpiredDate,
                            CERT_NUMBER = @CertCode,
                            NOTE = @Description
                        WHERE ID = @StaffCertId
                    END
                ELSE
                IF EXISTS (SELECT 1 FROM dbo.STAFF WHERE ID = @StaffId) AND EXISTS (SELECT 1 FROM dbo.CERTIFICATE WHERE ID = @CertId)
                    BEGIN
                        INSERT INTO dbo.STAFF_CERTIFICATE_REL
                        (
                            ID,
                            CERT_ID,
                            STAFF_ID,
                            DATE_OF_ISSUE,
                            DATE_EXPIRATION,
                            CERT_NUMBER,
                            NOTE,
                            DELETED,
                            LAST_UPDATE,
                            CREATED_DATE,
                            CREW_WORK_ID
                        )
                        VALUES
                        (
                            @StaffCertId,
                            @CertId,
                            @StaffId,
                            @IssueDate,
                            @ExpiredDate,
                            @CertCode,
                            @Description,
                            0,
                            GETDATE(),
                            GETDATE(),
                            0
                        )
                    END";
            _db.ExecuteNonQuery(
                sql,
                new SqlParameter("@StaffCertId", ToDbValue(staffCertId)),
                new SqlParameter("@CertId", ToDbValue(certId)),
                new SqlParameter("@CertCode", ToDbValue(certCode)),
                new SqlParameter("@IssueDate", ToDbValue(issueDate)),
                new SqlParameter("@ExpiredDate", ToDbValue(expiredDate)),
                new SqlParameter("@Description", ToDbValue(description)),
                new SqlParameter("@StaffId", ToDbValue(staffId))
            );

            if (!string.IsNullOrEmpty(attachFile))
            {
                if (IsStaffCertExisting(staffCertId))
                {
                    string extension = Path.GetExtension(attachFile);
                    string techFileName = GetStaffTechFileName(staffCertId);
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
                                IF EXISTS (SELECT 1 FROM dbo.STAFF_CERTIFICATE_REL WHERE ID = @StaffCertId)
                                    BEGIN
                                        UPDATE dbo.STAFF_CERTIFICATE_REL
                                        SET
                                            TECH_ATTACHED_FILE = @TechFileName,
                                            ATTACHED_FILE = @AttachdFile
                                        WHERE ID = @StaffCertId
                                    END";
                            _db.ExecuteNonQuery(
                               fileUpdateSql,
                               new SqlParameter("@StaffCertId", ToDbValue(staffCertId)),
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


    public class ImportStaffCertApiService
    {
        private readonly ApiHelper _apiHelper;
        private readonly string _url;
        private readonly int _pageSize;

        public ImportStaffCertApiService()
        {
            _apiHelper = new ApiHelper();
            _url = "http://nvtrans.lotusshipman.com/D04_Qualification/GetList";
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
                        ImportStaffCert importer = new ImportStaffCert();
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