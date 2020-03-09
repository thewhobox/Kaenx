using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Konnect.Responses
{
    public class ConnectResponse : IResponse
    {
        public ConnectResponse(byte headerLength, byte protocolVersion, ushort totalLength, byte communicationChannel,
      byte status, HostProtocolAddressInformation dataEndpoint, ConnectionResponseDataBlock connectionResponseDataBlock)
        {
            HeaderLength = headerLength;
            ProtocolVersion = protocolVersion;
            TotalLength = totalLength;
            CommunicationChannel = communicationChannel;
            Status = status;
            DataEndpoint = dataEndpoint;
            ConnectionResponseDataBlock = connectionResponseDataBlock;
        }

        public byte HeaderLength { get; }
        public byte ProtocolVersion { get; }
        public ushort TotalLength { get; }
        public byte CommunicationChannel { get; }
        public byte Status { get; }
        public HostProtocolAddressInformation DataEndpoint { get; }
        public ConnectionResponseDataBlock ConnectionResponseDataBlock { get; }
    }
}
