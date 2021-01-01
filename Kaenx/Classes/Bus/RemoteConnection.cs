using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus
{
    public class BusRemoteConnection
    {
        public static Kaenx.Konnect.Connections.RemoteConnection Instance { get; } = new Konnect.Connections.RemoteConnection();
    }
}
