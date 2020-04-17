using Kaenx.Konnect.Addresses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus
{
    public class BusInterface
    {
        public string Hash;
        public IPEndPoint Endpoint { get; set; }
        public string Name { get; set; }
        public DateTime LastFound { get; set; }
        public UnicastAddress Address { get; set; }
    }
}
