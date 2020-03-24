using Kaenx.Konnect.Addresses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus.Actions
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

        public List<AssociationHelper> AssociationTable { get; set; }
    }


    public class AssociationHelper
    {
        public string GroupIndex { get; set; }
        public int ObjectIndex { get; set; }
    }
}
