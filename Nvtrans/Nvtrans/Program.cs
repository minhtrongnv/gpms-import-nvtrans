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


        static void ImportMasterData()
        {
            MainMethod.ImportStaffBankFields();
            MainMethod.ImportStaffPosition();
            MainMethod.ImportStaffGraduation();
            MainMethod.ImportStaffRelation();
            MainMethod.ImportMasterStaffCert();
            MainMethod.ImportStaffAppraisalType();
        }

        static void Run()
        {
            //Master data Method
            //ImportMasterData();

            //Main Method
            //MainMethod.ImportStaffInfo();
            //MainMethod.ImportStaffCert();
            //MainMethod.ImportStaffRelative();
            MainMethod.ImportStaffAppraisal();



            //string filePath =
            //@"C:\Users\Docker\Desktop\trongnguyen\gpms-import-nvtrans\Nvtrans\Nvtrans\data_yaml\ship_mapping.yaml";

            //ShipMappingRepository repository =
            //    new ShipMappingRepository(filePath);

            //string sourceId = "5ea942d7-dde8-4c37-bc94-6df2b288bab4";

            //ShipMapping mapping = repository.GetById(sourceId);

            //if (mapping == null)
            //{
            //    Console.WriteLine("ID was not found.");
            //}
            //else
            //{
            //    Console.WriteLine("Name: " + mapping.OrgStructureName);

            //    if (mapping.MappingId.HasValue)
            //    {
            //        Console.WriteLine("Mapping ID: " + mapping.MappingId.Value);
            //    }
            //    else
            //    {
            //        Console.WriteLine("This object has no MappingId.");
            //    }
            //}
        }
    }
}