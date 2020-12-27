using Kaenx.DataContext.Local;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus
{
    public class BusInterfaceHelper
    {
        public static IKnxInterface GetInterface(LocalInterface inter)
        {
            IKnxInterface ninter = null;

            switch (inter.Type)
            {
                case InterfaceType.IP:
                    ninter = new KnxInterfaceIp()
                    {
                        Name = inter.Name,
                        Port = inter.Port,
                        IP = inter.Ip
                    };
                    break;


                case InterfaceType.USB:
                    ninter = new KnxInterfaceUsb()
                    {
                        Name = inter.Name,
                        DeviceId = inter.Ip
                    };
                    break;


                default:
                    throw new Exception("Not specified Interface Type: " + inter.Type);
            }

            return ninter;
        }

    }
}
