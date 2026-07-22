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
    public class ImportStaffExperience
    {
        private readonly SqlDb _db;
        private readonly string CompanyId = "A6F8FBCE-1F0F-4B38-B523-B77983129FF5";

        public ImportStaffExperience()
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

        public string GetPositionId(string positionId, string positionName)
        {
            string POSITION_ID = null;
            if (string.IsNullOrEmpty(positionId))
            {
                return POSITION_ID;
            }

            var positionMapping = new Dictionary<string, string>
            {
                { "d4b7e531-d064-45c8-acb7-13b0ef6c94e3", "A6F21098-F123-4F47-9D66-9539FD440A49" },
                { "edf3ed95-894b-46fb-8a4f-ed21653c4d56", "B4BFB5D4-4705-4C10-A5ED-D9ED2E465E84" },
                { "013b4d2f-acdd-477e-af2a-e85d00a0a24b", "8A214075-BFEA-4DE7-997E-C29895B8A644" },
                { "2a3c054f-ff7f-4c64-ba41-2bfa2c1a4782", "8FF3212D-3A39-49FF-9A38-9D04A42F0515" },
                { "e87060c1-a1ef-4d50-a3f8-d667d8126f21", "76BCFF47-B211-4CF9-8370-33069D5FCD17" },
                { "3142b6b9-40de-45b5-a032-85fc6d2d8aec", "36351742-FE6A-4AAA-AD6C-41B946DAA783" },
                { "962eaeac-5d98-4b58-ada1-65de4145a123", "FA373BC1-2A04-4F8F-932D-B1143B57EE7E" },
                { "55ecbae4-88ea-43af-bc2d-ae69108a180f", "53BD915E-EF71-4AC0-B759-625D6BA87729" },
                { "37d7cfe6-f8ac-4571-a771-2a89247e06cd", "C8291F0C-65AC-401E-8E0B-2244C87F34F4" },
                { "ae31ec24-670e-45f9-b954-c46461163d8d", "113E47C5-3F50-4273-B92C-B4C05D776495" },
                { "810a0042-e263-4c41-806c-2fdf9a4cc5a7", "FF947BB0-E36E-41C4-B9E3-7952EE7FCF0B" },
                { "2d732c06-7f7c-4e2d-ac8e-f1950d9ae09d", "8FF3212D-3A39-49FF-9A38-9D04A42F0515" },
                { "fff970e7-0b56-4fbe-a0ef-46207119b2de", "210AD0F6-3777-41F7-AE8F-D17D1DF8F378" },
                { "81da68e4-be99-4468-ac08-13606505b517", "2D7FA39D-838A-4A35-B31E-CD18AD8F7A96" },
                { "6d72cec7-9245-4c7c-b3c2-f12db14c8acb", "8B44CAAE-FB7F-4148-ACF0-B3DB3B7D9890" },
                { "b967b6b6-74db-4fc8-a84d-120578aa9fb2", "CE87E692-D2B2-40D5-8891-6B55D6A9061F" },
                { "80c80a42-c2f9-45c3-83d9-af2653d9e427", "132DC90F-E9CF-4614-9CAA-357C6C9635C3" },
                { "9e1a832d-753c-407c-bb9c-00e1168ba440", "C40FCA9E-933D-4D75-BBD9-08DFE9700F58" },
                { "a74e4480-90a5-4f4c-9c05-5bfc58b3b7d7", "A5C93F1F-A7F3-4B81-8307-283E9D388416" },
                { "0562664e-5d31-4f28-bfac-f869dfc7f2a1", "3A62912E-19F6-4341-B6E2-9208FB5726E7" },
                { "749ad325-eb9d-463e-91e8-a9532e71ee7b", "36D1076F-09B9-4B91-AC91-4449271A92E0" },
                { "84dff078-7f61-416b-8ba8-f8e627cfb764", "5A6AD9F2-3BC5-44AF-8E22-CDDFCCC168E5" },
                { "9cba35ee-2e41-42a0-aad5-d5495d40dede", "9DAD64A3-4897-4543-B752-3820D1BB4B71" },
                { "61870d6b-7b97-4e2a-9b9c-cc7736eeb17a", "EA859D46-5674-4420-888B-63CD377B2BDE" },
                { "ab3b643d-d4d3-48e1-bcd1-c40971d2c84f", "8C0F135F-8826-416F-8D23-CABCFE660670" },
                { "cc88b555-f53c-44eb-ae40-9a3e54e04c4e", "9DAD64A3-4897-4543-B752-3820D1BB4B71" },
                { "d525ce9f-0779-4715-a099-7dd549621dbb", "DEA84830-51A0-4C59-B2B9-DDC9C59459E6" },
                { "6e97ec71-07e8-4dd7-b24b-785e03cfb7c7", "3B1FF31C-831A-45B4-82C2-2E539A579967" },
                { "0cc0ad2e-7fe5-426f-ad0b-70c7b1278a6d", "05069121-2363-4377-93A1-D70E636E92CA" },
                { "dbda4cd0-812d-4b64-9151-5147825de77f", "FE53A808-8F0C-499C-BFE9-8745A97BB812" },
                { "265b4067-a617-4b58-a187-43314ec844b4", "7EE9F99C-F9D2-44E6-B528-59921DBDB03E" },
                { "dbbe18ed-3e5a-40cd-bc6c-3c8a3c7437d8", "0671C309-00D6-477B-84F3-8AEE5B29C5DF" },
                { "f6f56e14-aa33-425b-9bd5-2085a8340840", "541B7780-A27E-4D89-88C5-5E1BBFA989CE" },
                { "e1167e23-515c-4985-8b5a-1d0ab714fb56", "55E5DB39-E71C-4E6B-9EE1-AAF0B689DAF8" },
                { "19a722b5-75b3-4dd4-adcf-15435782700c", "C2628708-4319-45B8-AB05-9C2AEE16C597" },
                { "5236346d-22ab-4a73-b959-152d1686c1ae", "7EE9F99C-F9D2-44E6-B528-59921DBDB03E" },
                { "341da340-8a93-4073-b65c-15033f786136", "B25D4D43-B739-4611-8D14-BD0C389066F4" }
            };

            if (!string.IsNullOrEmpty(positionId))
            {
                positionMapping.TryGetValue(positionId, out POSITION_ID);
            }

            if (string.IsNullOrEmpty(POSITION_ID))
            {
                const string sql = @"
                    IF NOT EXISTS
                    (
                        SELECT 1
                        FROM dbo.POSITION
                        WHERE ID = @PositionId
                    )
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
                                LAST_UPDATE,
                                CREATED_DATE
                            )
                            VALUES
                            (
                                @PositionId,
                                'NewCodePosition',
                                @PositionName,
                                @CompanyId,
                                0,                          
                                0,
                                0,
                                0,
                                GETDATE(),
                                GETDATE()
                            )
                        END";

                object result = _db.ExecuteScalar(
                    sql,
                    new SqlParameter("@PositionId", positionId),
                    new SqlParameter("@PositionName", positionName),
                    new SqlParameter("@CompanyId", CompanyId)
                );

                if (string.IsNullOrEmpty(POSITION_ID))
                {
                    const string sql2 = @"
                    SELECT TOP 1 ID
                    FROM dbo.POSITION
                    WHERE ID = @PositionId";

                    object result2 = _db.ExecuteScalar(
                        sql2,
                        new SqlParameter("@PositionId", positionId)
                    );

                    if (result2 != null && result2 != DBNull.Value)
                        POSITION_ID = result2.ToString();
                }
            }

            return POSITION_ID;
        }

        public string GetShipId(string structureId)
        {
            string StructureId = null;
            var orgStructureMapping = new Dictionary<string, string>
            {
                { "96b6b8e2-56a9-452b-9b9b-017e1cd234da", "559E78F0-F972-4D78-8788-8B466DCB5183" },
                { "5361bad8-5dda-4b5f-b547-0c1419ebcfd9", "627A8613-1357-47EB-A3CE-F888C4BE39CE" },
                { "22643c12-84d0-433f-a60c-10c9b44244e7", "8494A072-E90F-4D1F-90E9-0A37A0FB929E" },
                { "88c134ad-8153-4f0f-9d6f-12edbfc58fb4", "00000000-0000-0000-0000-000000000003" },
                { "b82eb1b4-9e35-4d84-b387-275a33ab4d96", "784A86CF-1F0B-4672-85D1-FC85FC7A5019" },
                { "829cffaf-accf-4615-8fc0-23af1d23037d", "CE5F279F-30AF-4929-9C6D-E6166D113877" },
                { "107d77a1-772a-41e8-8a7a-491f8168b711", "8F54BEEB-81A7-4415-B7A9-A41726BDB368" },
                { "6bce23e0-b7e4-464d-b794-5afcf7f9ac82", "5C9478FE-CFCA-4225-A880-1A1522BE9609" },
                { "92863f01-f1d3-4817-bca7-70302f8360b2", "591CAF1C-75EB-4BC4-B949-875316C2926C" },
                { "5bc6c99b-3504-41ce-9676-71153e93689a", "81092C7C-14B3-4D34-ABC9-2DB568B21975" },
                { "1b32c5bb-a741-425f-86dc-79d31d85ab65", "78E19DAB-82B2-4D02-B312-CDA3ECC0591D" },
                { "eb5e7e15-4277-465e-84dd-992dc02291f4", "8C92F3BC-D144-4789-89DE-4F7DD08EA61B" },
                { "5d754e6d-d0f0-4cd1-bfcc-a0d87cdd3886", "C54768B0-5D77-47BC-B165-C88AE9C2228B" },
                { "fe618df5-a590-4828-8ec9-a2141d759032", "8704B0AF-47FD-4011-9908-BDC9055A602C" },
                { "11321c83-f605-4c2a-a03a-b0f901b1ab67", "141F79A0-1085-4EC4-A9C9-6A37A4C7F5C1" },
                { "47f9cacb-3135-4aa5-a5d7-c447608238cf", "00000000-0000-0000-0000-000000000003" },
                { "85c3f789-8eba-41cf-a160-e7f9a711c43b", "7DDA8C9E-3098-49A9-8450-EA5EC0866D4B" },
                { "1c2e0b44-4d1b-4f50-8df9-f5b2952ba6cb", "F8372B19-5E5E-42AA-AB5C-D1D862104168" },
                { "7c1d2fd0-41ec-4d20-ad1d-c17d12144554", "F4E06F63-9502-4E7E-A773-DCAED6457474" },
                { "b3cd62bd-a64e-4254-a1e0-b7355e0db32f", "92D16D4C-9919-4E5D-B647-A184F9F9C693" },
                { "98eb6049-88e1-4aec-9098-3bb33f576ae9", "7DDA8C9E-3098-49A9-8450-EA5EC0866D4B" },
                { "23a74322-872f-48e8-b490-873706ba30f7", "08508FFC-1964-4345-B275-20BB7D07B655" },
                { "71806e34-b98e-47ad-8302-6ae963635e71", "59A64B7E-20E5-4C5E-B649-E0F6F47C13CF" },
                { "c351bcdb-e8db-4d94-9a4c-861504811b9b", "00000000-0000-0000-0000-000000000005" },
                { "28d86afc-b911-4b0a-b228-bd378392b923", "AD7F27E7-AC7A-4010-BB08-D4CE29CF7DA4" },
                { "4676e5b6-8d84-4a65-919b-f7ba225f8212", "F1FE8CAE-0C7B-4ECA-917F-02543EFAE422" },
                { "41ee1009-0b2c-4d1a-98a4-f0bae0347734", "5E9F6890-C814-482C-B562-C6B12AE63CAF" },
                { "e3c1705c-0a7f-4a84-b348-7f26ec8b8179", "DB6362DD-8B36-41EB-9036-85D334CB7C95" },
                { "5f1e6c02-e02b-4a2c-b628-692cec3e7f7a", "4578731C-1784-4D21-B4A0-672393A5AA3E" },
                { "82f1ef90-8946-4de9-a77d-81773df448b9", "27776B5F-C2CE-4E11-8FB9-8E6A10DAFCC5" }
            };

            if (!string.IsNullOrEmpty(structureId))
            {
                orgStructureMapping.TryGetValue(structureId, out StructureId);
            }

            return StructureId;
        }

        public string GetDepositionId(string positionId, string shipId, string shipName)
        {
            string DepositionId = null;
            if (shipId != "00000000-0000-0000-0000-000000000003")
            {
                const string sql = @"
                    IF NOT EXISTS
                    (
                        SELECT 1
                        FROM dbo.DEPT_POSITION
                        WHERE POS_ID = @PositionId
                          AND DEPT_ID = @ShipId
                          AND DELETED = 0
                    )
                        BEGIN
                            INSERT INTO dbo.DEPT_POSITION
                            (
                                ID,
                                DEPT_ID,
                                POS_ID,
                                DAY_ON_BOARD,
                                ALERT_LEVEL1,
                                ALERT_LEVEL2,
                                SHIP_NAME,
                                DELETED,
                                LAST_UPDATE,
                                CREATED_DATE
                            )
                            VALUES
                            (
                                NEWID(),
                                @ShipId,
                                @PositionId,
                                0,
                                0,
                                0,
                                @ShipName,
                                0,
                                GETDATE(),
                                GETDATE()
                            )
                        END";

                _db.ExecuteScalar(
                    sql,
                    new SqlParameter("@PositionId", positionId),
                    new SqlParameter("@ShipId", shipId),
                    new SqlParameter("@ShipName", shipName)
                );

                const string sql2 = @"
                    SELECT TOP 1 ID
                    FROM dbo.DEPT_POSITION
                    WHERE POS_ID = @PositionId AND DELETED = 0 AND DEPT_ID = @ShipId";

                object result2 = _db.ExecuteScalar(
                    sql2,
                    new SqlParameter("@PositionId", positionId),
                    new SqlParameter("@ShipId", shipId)
                );

                if (result2 != null && result2 != DBNull.Value)
                {
                    DepositionId = result2.ToString();
                }
            }
            return DepositionId;
        }

        public string GetShipName(string shipId)
        {
            string ShipName = "";
            if (shipId == "00000000-0000-0000-0000-000000000003")
            {
                ShipName = "SLEEP/LEAVED";
            } 
            else
            {
                const string sql = @"
                    SELECT TOP 1 NAME
                    FROM dbo.SHIP
                    WHERE ID = @ShipId";

                object result = _db.ExecuteScalar(
                    sql,
                    new SqlParameter("@ShipId", shipId)
                );

                if (result != null && result != DBNull.Value)
                    ShipName = result.ToString();
            }
            return ShipName;
        }

        public void InsertOrUpdate(JObject experience)
        {
            string experienceId = GetString(experience, "ID");
            string staffId = GetString(experience, "EmployeeID");
            string positionId = GetPositionId(GetString(experience, "PositionID"), GetString(experience, "PositionName"));
            string shipId = GetShipId(GetString(experience, "OrgStructureID"));
            string startDate = ParseDateToSqlString(GetString(experience, "DateEffective"));
            string endDate = ParseDateToSqlString(GetString(experience, "DateExpiry"));
            string remark = GetString(experience, "Description");
            string shipName = !string.IsNullOrEmpty(shipId) ? GetShipName(shipId) : GetString(experience, "OrgStructureName");
            string depositionId = !string.IsNullOrEmpty(positionId) && !string.IsNullOrEmpty(shipId) ? GetDepositionId(positionId, shipId, shipName) : null;
            string companyName = !string.IsNullOrEmpty(shipId) ? "NVTRANS" : null;
            string sql = @"
                IF EXISTS (SELECT 1 FROM dbo.CREW_HISTORY WHERE ID = @ExperienceId)
                    BEGIN
                        UPDATE dbo.CREW_HISTORY
                        SET
                            SHIP_ID = @ShipId,
                            DATE_START = @StartDate,
                            DATE_END = @EndDate,
                            SHIP_NAME = @ShipName,
                            POSITION_ID = @PositionId,
                            COMPANY_NAME = @CompanyName,
                            REMARK = @Remark,
                            DEPT_POSITION_ID = @DepPositionId
                        WHERE ID = @ExperienceId
                    END
                ELSE
                IF EXISTS (SELECT 1 FROM dbo.STAFF WHERE ID = @StaffId)
                    BEGIN
                        INSERT INTO dbo.CREW_HISTORY
                        (
                            ID,
                            STAFF_ID,
                            SHIP_ID,
                            DATE_START,
                            DATE_END,
                            SHIP_NAME,
                            POSITION_ID,
                            COMPANY_NAME,
                            REMARK,
                            DEPT_POSITION_ID,
                            CREW_WORK_ID,
                            DELETED,
                            LAST_UPDATE,
                            CREATED_DATE
                        )
                        VALUES
                        (
                            @ExperienceId,
                            @StaffId,
                            @ShipId,
                            @StartDate,
                            @EndDate,
                            @ShipName,
                            @PositionId,
                            @CompanyName,
                            @Remark,
                            @DepPositionId,
                            0,
                            0,
                            GETDATE(),
                            GETDATE()
                        )
                    END";
            _db.ExecuteNonQuery(
                sql,
                new SqlParameter("@ExperienceId", ToDbValue(experienceId)),
                new SqlParameter("@StaffId", ToDbValue(staffId)),
                new SqlParameter("@ShipId", ToDbValue(shipId)),
                new SqlParameter("@StartDate", ToDbValue(startDate)),
                new SqlParameter("@EndDate", ToDbValue(endDate)),
                new SqlParameter("@ShipName", ToDbValue(shipName)),
                new SqlParameter("@PositionId", ToDbValue(positionId)),
                new SqlParameter("@CompanyName", ToDbValue(companyName)),
                new SqlParameter("@Remark", ToDbValue(remark)),
                new SqlParameter("@DepPositionId", ToDbValue(depositionId))
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


    public class ImportStaffExperienceApiService
    {
        private readonly ApiHelper _apiHelper;
        private readonly string _url;
        private readonly int _pageSize;

        public ImportStaffExperienceApiService()
        {
            _apiHelper = new ApiHelper();
            _url = "http://nvtrans.lotusshipman.com/D04_WorkHistory/GetList";
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
                        ImportStaffExperience importer = new ImportStaffExperience();
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