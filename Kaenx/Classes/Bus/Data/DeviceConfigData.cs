using Kaenx.DataContext.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus.Data
{
    public class DeviceConfigData : IBusData
    {
        public Dictionary<string, AppParameter> Parameters { get; set; }
        public string Type { get; set; } = "Konfiguration";
        public LineDevice Device { get; set; }
        public string Manufacturer { get; set; }
        public string SerialNumber { get; set; }
        public string ApplicationName { get; set; }
        public string ApplicationId { get; set; }
        public string MaskVersion { get; set; }
    }
}
