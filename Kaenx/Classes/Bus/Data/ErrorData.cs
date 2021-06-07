using Kaenx.Classes.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus.Data
{
    public class ErrorData : IBusData
    {
        public string Type { get; } = "Error";
        public LineDevice Device { get; set; }
        public string Manufacturer { get; set; }
        public string SerialNumber { get; set; }
        public string ApplicationName { get; set; }
        public string Additional { get; set; }

        public ErrorData() { }

        public ErrorData(IBusData data, string msg)
        {
            Device = data.Device;
            ApplicationName = data.ApplicationName;
            Manufacturer = data.Manufacturer;
            SerialNumber = data.SerialNumber;
            Additional = msg;
        }
    }
}
