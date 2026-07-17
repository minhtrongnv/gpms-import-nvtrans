using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nvtrans
{
    public class MainMethod
    {
        public static void ImportStaffInfo()
        {
            ImportStaffApiService staffApiService = new ImportStaffApiService();
            List<JObject> staffList = staffApiService.GetAllDataAsync().GetAwaiter().GetResult();
            ImportStaff importer = new ImportStaff();

            int count = 0;

            foreach (JObject staff in staffList)
            {
                importer.InsertOrUpdate(staff);
                count++;
            }
        }

        public static void ImportStaffCert()
        {
            ImportStaffCertApiService staffApiService = new ImportStaffCertApiService();
            List<JObject> staffList = staffApiService.GetAllDataAsync().GetAwaiter().GetResult();
        }

        public static void ImportStaffPosition()
        {
            string filePath = ConfigurationManager.AppSettings["StaffPositionFilePath"];
            var importer = new ImportMasterPosition(); 
            importer.ImportFromFile(filePath);
        }

        public static void ImportStaffGraduation()
        {
            ImportMasterGraduationApiSevice graduationApiService = new ImportMasterGraduationApiSevice();
            List<JObject> graduationList = graduationApiService.GetAllDataAsync().GetAwaiter().GetResult();
            ImportMasterGraduation importer = new ImportMasterGraduation();

            int count = 0;

            foreach (JObject graduation in graduationList)
            {
                importer.InsertOrUpdate(graduation);
                count++;
            }
        }
        public static void ImportStaffRelation()
        {
            ImportMasterStaffRelation relationApiService = new ImportMasterStaffRelation();
            relationApiService.InsertOrUpdate();
        }

        public static void ImportStaffBankFields()
        {
            ImportMasterBankAccountField relationApiService = new ImportMasterBankAccountField();
            relationApiService.InsertOrUpdate();
        }

        public static void ImportMasterStaffCert()
        {
            ImportMasterStaffCertService staffCertApiService = new ImportMasterStaffCertService();

            List<JObject> staffCertList = staffCertApiService.GetAllDataAsync().GetAwaiter().GetResult();
            ImportMasterStaffCert importer = new ImportMasterStaffCert();

            int count = 0;

            foreach (JObject cert in staffCertList)
            {
                importer.InsertOrUpdate(cert);
                count++;
            }
        }
    }
}
