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
            //Import active staff
            ImportStaffApiService staffApiService = new ImportStaffApiService(false);
            List<JObject> staffList = staffApiService.GetAllDataAsync().GetAwaiter().GetResult();
            ImportStaff importer = new ImportStaff();

            int count = 0;

            foreach (JObject staff in staffList)
            {
                importer.InsertOrUpdate(staff);
                count++;
            }

            //Import terminated staff
            ImportStaffApiService terminated = new ImportStaffApiService(true);
            List<JObject> terminatedLst = terminated.GetAllDataAsync().GetAwaiter().GetResult();
            ImportStaff terminatedImporter = new ImportStaff(true);

            int terminatedCount = 0;

            foreach (JObject staff in terminatedLst)
            {
                terminatedImporter.InsertOrUpdate(staff);
                terminatedCount++;
            }
        }

        public static void ImportStaffRelative()
        {
            ImportStaffRelatedApiService staffApiService = new ImportStaffRelatedApiService();
            List<JObject> result = staffApiService.GetAllDataAsync().GetAwaiter().GetResult();
        }

        public static void ImportStaffAppraisal()
        {
            ImportStaffAppraisalApiService staffApiService = new ImportStaffAppraisalApiService();
            List<JObject> staffList = staffApiService.GetAllDataAsync().GetAwaiter().GetResult();
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


        public static void ImportStaffAppraisalType()
        {
            ImportMasterStaffAppraisalType service = new ImportMasterStaffAppraisalType();
            service.InsertOrUpdate();
        }

        public static void ImportStaffBankFields()
        {
            ImportMasterBankAccountField relationApiService = new ImportMasterBankAccountField();
            relationApiService.InsertOrUpdate();
        }

        public static void ImportStaffContract()
        {
            ImportStaffContractApiService service = new ImportStaffContractApiService();
            var staffCertList = service.GetAllDataAsync().GetAwaiter().GetResult();
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
