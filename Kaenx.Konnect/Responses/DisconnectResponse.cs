using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Konnect.Responses
{
    public class DisconnectResponse : IResponse
    {
        public DisconnectResponse(byte headerLength, byte protocolVersion, ushort totalLength, byte communicationChannel,
      byte status)
        {
            HeaderLength = headerLength;
            ProtocolVersion = protocolVersion;
            TotalLength = totalLength;
            CommunicationChannel = communicationChannel;
            Status = status;
        }

        public byte HeaderLength { get; }
        public byte ProtocolVersion { get; }
        public ushort TotalLength { get; }
        public byte CommunicationChannel { get; }
        public byte Status { get; }
    }
}
