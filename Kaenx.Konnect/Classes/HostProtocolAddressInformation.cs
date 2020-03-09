using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Kaenx.Konnect.Classes
{
    public class HostProtocolAddressInformation
    {
        public HostProtocolAddressInformation(byte hostProtocolCode, IPEndPoint ipEndPoint)
        {
            StructureLength = 0x08;
            HostProtocolCode = hostProtocolCode;
            IpEndPoint = ipEndPoint;
        }

        public byte StructureLength { get; }
        public byte HostProtocolCode { get; }
        public IPEndPoint IpEndPoint { get; }

        public byte[] GetBytes()
        {
            var result = new byte[8];
            result[0] = StructureLength;
            result[1] = HostProtocolCode;
            var addressBytes = IpEndPoint.Address.GetAddressBytes();

            result[2] = addressBytes[0];
            result[3] = addressBytes[1];
            result[4] = addressBytes[2];
            result[5] = addressBytes[3];

            var port = BitConverter.GetBytes((ushort)IpEndPoint.Port);

            result[6] = port[1];
            result[7] = port[0];

            return result;
        }
    }
}
