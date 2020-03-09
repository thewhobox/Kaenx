using Kaenx.Konnect.Classes;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Kaenx.Konnect.Builders
{
    public class DisconnectRequest : IRequestBuilder
    {
        private List<byte> bytes = new List<byte>();

        public void Build(IPEndPoint source, byte communitycationChannel)
        {
            byte[] header = { 0x06, 0x10, 0x02, 0x09, 0x00, 0x10 }; // Length, Version, Descriptor 2x, Total length 2x
            bytes.AddRange(header);

            bytes.Add(communitycationChannel); // Channel
            bytes.Add(0x00); //Status OK
            bytes.AddRange(new HostProtocolAddressInformation(0x01, source).GetBytes());
        }

        public byte[] GetBytes()
        {
            return bytes.ToArray();
        }

        public void SetChannelId(byte channelId) 
        {
            
        }

        public void SetSequence(byte sequence) { }

        public void SetSequenzCounter(byte sequenzCounter) { }
    }
}
