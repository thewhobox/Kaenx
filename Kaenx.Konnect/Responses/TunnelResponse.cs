using Kaenx.Konnect.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Konnect.Responses
{
    public class TunnelResponse : IResponse
    {
        public TunnelResponse(byte headerLength, byte protocolVersion, ushort totalLength, byte structureLength,
          byte communicationChannel, byte sequenceCounter, byte status)
        {
            HeaderLength = headerLength;
            ProtocolVersion = protocolVersion;
            TotalLength = totalLength;
            StructureLength = structureLength;
            CommunicationChannel = communicationChannel;
            SequenceCounter = sequenceCounter;
            Status = status;
        }

        public byte HeaderLength { get; }
        public byte ProtocolVersion { get; }
        public ushort TotalLength { get; }
        public byte StructureLength { get; }
        public byte CommunicationChannel { get; }
        public byte SequenceCounter { get; }
        public byte Status { get; }

        public byte[] GetBytes()
        {
            var bytes = new byte[TotalLength];
            var totalLength = BitConverter.GetBytes(TotalLength);
            bytes[0] = HeaderLength;
            bytes[1] = ProtocolVersion;
            bytes[2] = 0x04;
            bytes[3] = 0x21;
            bytes[4] = totalLength[1];
            bytes[5] = totalLength[0];
            bytes[6] = StructureLength;
            bytes[7] = CommunicationChannel;
            bytes[8] = SequenceCounter;
            bytes[9] = Status;

            return bytes;
        }
    }

    public static class ByteArrayExtensions
    {
    }
}
