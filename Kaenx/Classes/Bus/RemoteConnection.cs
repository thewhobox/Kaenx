using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus
{
    public class BusRemoteConnection
    {
        public static Kaenx.Konnect.Remote.RemoteConnection Instance { get; } = new Konnect.Remote.RemoteConnection();
    }
}
