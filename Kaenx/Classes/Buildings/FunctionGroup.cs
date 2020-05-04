using Kaenx.Classes.Project;
using Kaenx.Konnect.Addresses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Buildings
{
    public class FunctionGroup
    {
        public MulticastAddress Address { get; set; }
        public GroupAddress GroupAddress { get; set; }
        public string Name { get; set; }
    }
}
