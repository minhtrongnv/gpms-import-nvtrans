using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Nvtrans
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Run();
                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.ToString());
            }

            Console.ReadLine();
        }

        static void Run()
        {
            //Import Staff Relation
            //ImportMasterStaffRelation relationApiService = new ImportMasterStaffRelation();
            //relationApiService.InsertOrUpdate();




            //Import Education
            //ImportMasterEducationApiService educationApiService = new ImportMasterEducationApiService();

            //List<JObject> educationList = educationApiService.GetAllDataAsync().GetAwaiter().GetResult();
            //ImportMasterEducation importer = new ImportMasterEducation();

            //int count = 0;

            //foreach (JObject education in educationList)
            //{
            //    importer.InsertOrUpdate(education);
            //    count++;
            //}





            //Import Graduation
            //ImportMasterGraduationApiSevice graduationApiService = new ImportMasterGraduationApiSevice();

            //List<JObject> graduationList = graduationApiService.GetAllDataAsync().GetAwaiter().GetResult();
            //ImportMasterGraduation importer = new ImportMasterGraduation();

            //int count = 0;

            //foreach (JObject graduation in graduationList)
            //{
            //    importer.InsertOrUpdate(graduation);
            //    count++;
            //}





            //Import Staff Info
            //ImportStaffApiService staffApiService = new ImportStaffApiService();

            //List<JObject> staffList = staffApiService.GetAllDataAsync().GetAwaiter().GetResult();
            //ImportStaff importer = new ImportStaff();

            //int count = 0;

            //foreach (JObject staff in staffList)
            //{
            //    importer.InsertOrUpdate(staff);
            //    count++;
            //}
        }
    }
}