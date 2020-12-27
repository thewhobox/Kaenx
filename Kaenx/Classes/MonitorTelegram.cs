using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes
{
    public class MonitorTelegram
    {
        public IKnxAddress From { get; set; }
        public IKnxAddress To { get; set; }
        public string Data { get; set; }
        public DateTime Time { get; set; }
        public ApciTypes Type { get; set; }
    }
}
