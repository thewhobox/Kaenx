using Device.Net;
using Hid.Net.UWP;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus
{
    public class BusRemoteConnection
    {
        //public static Kaenx.Konnect.Connections.RemoteConnection Instance { get; } = new Konnect.Connections.RemoteConnection(BusConnection.Instance.GetDevice);
        public static BusRemoteConnection Instance { get; } = new BusRemoteConnection();
        public RemoteConnection Remote { get; }

        private IDeviceFactory hidFactory = new FilterDeviceDefinition(vendorId: 0x147b).CreateUwpHidDeviceFactory();

        private async Task<IDevice> GetDevice(KnxInterfaceUsb inter)
        {
            return await hidFactory.GetDeviceAsync(inter.ConnDefinition);
        }

        public BusRemoteConnection()
        {
            Remote = new RemoteConnection(GetDevice);
        }
    }
}
