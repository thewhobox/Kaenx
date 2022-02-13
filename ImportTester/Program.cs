using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Import;
using Kaenx.DataContext.Import.Manager;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace ImportTester
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> files = new List<string>()
            {
                @"C:\Users\mikeg\Desktop\KONNEKTING_ALEDD1.kdevice.xml",
                @"C:\Users\mikeg\OneDrive - Mike Gerst\KNX\KNX-Prods\MDT_KP_BE_01_Push_Button_V15a.knxprod"
            };

            int index = 0;
            foreach (string file in files)
            {
                string fileName = new System.IO.FileInfo(file).Name;
                Console.WriteLine($"[{index++}] {fileName}");
            }
            Console.Write("Datei wählen: ");
            index = int.Parse(Console.ReadLine());
            Console.WriteLine();

            IManager manager = ImportManager.GetImportManager(files[index]);
            var langs = manager.GetLanguages();
            manager.SetLanguage(langs[0]);
            var x = manager.GetDeviceList();


            Console.WriteLine();
            index = 0;
            foreach(ImportDevice device in x)
            {
                Console.WriteLine($"[{index++}] {device.Name} ({device.Description})");
            }
            Console.Write("Gerät wählen: ");
            index = int.Parse(Console.ReadLine());

            Console.WriteLine("\r\nBeginne mit dem Import von '" + x[index].Name  + "'");
            manager.DeviceChanged += Manager_DeviceChanged;
            manager.StateChanged += Manager_StateChanged;

            CatalogContext context = new CatalogContext();
            context.Database.Migrate();

            manager.StartImport(x[index], context);
        }

        private static void Manager_StateChanged(string newState)
        {
            Console.WriteLine("State: " + newState);
        }

        private static void Manager_DeviceChanged(string newName)
        {
            Console.WriteLine(newName);
        }
    }
}
