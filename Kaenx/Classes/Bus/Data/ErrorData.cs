using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus.Data
{
    public class ErrorData : IBusData
    {
        public string Type { get; set; }
        public LineDevice Device { get; set; }
        public string Manufacturer { get; set; }
        public string SerialNumber { get; set; }
        public string ApplicationName { get; set; }
        public string Message { get; set; }

        public ErrorData() { }

        public ErrorData(IBusData data, string msg)
        {
            Type = data.Type;
            Device = data.Device;
            ApplicationName = data.ApplicationName;
            Manufacturer = data.Manufacturer;
            SerialNumber = data.SerialNumber;
            Message = msg;
        }
    }
}
