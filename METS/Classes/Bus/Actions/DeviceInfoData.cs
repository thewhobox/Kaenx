using METS.Knx.Addresses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.Classes.Bus.Actions
{
    public class DeviceInfoData
    {
        public string MaskVersion { get; set; }
        public string SerialNumber { get; set; }
        public string ApplicationId { get; set; }
        public string ApplicationName { get; set; }
        public string Manufacturer { get; set; }
        public string DeviceName { get; set; }
        public LineDevice Device { get; set; }


        public List<MulticastAddress> GroupTable { get; set; }
    }
}
