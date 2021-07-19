using Kaenx.DataContext.Import;
using Kaenx.DataContext.Import.Manager;
using System;

namespace ImportTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine("Getting downloader");
            IManager manager = ImportManager.GetImportManager(@"C:\Users\mikeg\OneDrive - Mike Gerst\KNX\KNX-Prods\MDT_KP_BE_01_Push_Button_V15a.knxprod");
        }
    }
}
