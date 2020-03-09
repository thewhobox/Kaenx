using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Kaenx.Konnect.Builders
{
    public class ConnectionRequest : IRequestBuilder
    {
        private List<byte> bytes = new List<byte>();

        public void Build(IPEndPoint source, byte communicationChannel)
        {
            byte[] header = { 0x06, 0x10, 0x02, 0x05 };
            bytes.AddRange(header);

            // Connection HPAI
            bytes.Add(0x08); // Body Structure Length
            bytes.Add(0x01); // IPv4
            bytes.AddRange(source.Address.GetAddressBytes()); // IP Address
            byte[] port = BitConverter.GetBytes((ushort)source.Port);
            Array.Reverse(port);
            bytes.AddRange(port); // IP Adress Port

            // Tunnelling HPAI
            bytes.Add(0x08); // Body Structure Length
            bytes.Add(0x01); // IPv4
            bytes.AddRange(source.Address.GetAddressBytes()); // IP Address
            port = BitConverter.GetBytes((ushort)source.Port);
            Array.Reverse(port);
            bytes.AddRange(port); // IP Adress Port


            bytes.Add(0x04); // Request Structure Length
            bytes.Add(0x04); // Tunnel Connection
            bytes.Add(0x02); // Tunnel Link Layer
            bytes.Add(0x00); // Reserved

            byte[] length = BitConverter.GetBytes((ushort)(bytes.Count + 2));
            Array.Reverse(length);
            bytes.InsertRange(4, length);
        }

        public byte[] GetBytes()
        {
            return bytes.ToArray();
        }

        public void SetChannelId(byte channelId) { }

        public void SetSequence(byte sequence) { }

        public void SetSequenzCounter(byte sequenzCounter) { }
    }
}
