using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Parser;
using Kaenx.Konnect.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Konnect.Builders
{
    public class TunnelResponse : IResponse
    {
        public TunnelResponse(byte headerLength, byte protocolVersion, ushort totalLength, byte structureLength,
          byte communicationChannel, byte sequenceCounter, byte messageCode, byte addInformationLength, byte controlField,
          byte controlField2, UnicastAddress sourceAddress, MulticastAddress destinationAddress, ApciTypes apci, int seqNumb,
          byte[] data)
        {
            HeaderLength = headerLength;
            ProtocolVersion = protocolVersion;
            TotalLength = totalLength;
            StructureLength = structureLength;
            CommunicationChannel = communicationChannel;
            SequenceCounter = sequenceCounter;
            MessageCode = messageCode;
            AddInformationLength = addInformationLength;
            ControlField = controlField;
            ControlField2 = controlField2;
            SourceAddress = sourceAddress;
            DestinationAddress = destinationAddress;
            APCI = apci;
            SequenceNumber = seqNumb;
            Data = data;
        }

        public byte HeaderLength { get; }
        public byte ProtocolVersion { get; }
        public ushort TotalLength { get; }
        public byte StructureLength { get; }
        public byte CommunicationChannel { get; }
        public byte SequenceCounter { get; }
        public int SequenceNumber { get; }
        public byte MessageCode { get; }
        public byte AddInformationLength { get; }
        public byte ControlField { get; }
        public byte ControlField2 { get; }
        public UnicastAddress SourceAddress { get; }
        public object DestinationAddress { get; }
        public ApciTypes APCI { get; }
        public byte[] Data { get; }
    }
}
