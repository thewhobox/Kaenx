using Kaenx.Konnect.Addresses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Konnect.Responses
{
    public class ConnectionResponseDataBlock
    {
        public ConnectionResponseDataBlock(byte structureLength, byte connectionType, UnicastAddress knxAddress)
        {
            StructureLength = structureLength;
            ConnectionType = connectionType;
            KnxAddress = knxAddress;
        }

        public byte StructureLength { get; }
        public byte ConnectionType { get; }
        public UnicastAddress KnxAddress { get; }
    }
}
