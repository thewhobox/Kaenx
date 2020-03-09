using Kaenx.Konnect.Addresses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus
{
    public class BusSettings
    {
        public string IpAddress { get; set; }
        public int IpPort { get; set; }
        public string PhAddress { get; set; }
    }
}
