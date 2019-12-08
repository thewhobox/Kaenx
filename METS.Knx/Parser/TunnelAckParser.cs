using METS.Knx.Addresses;
using METS.Knx.Classes;
using METS.Knx.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.Knx.Parser
{
    public class TunnelAckParser : IReceiveParser
    {
        public ushort ServiceTypeIdentifier => 0x0421;

        public TunnelAckParser() { }

        IResponse IReceiveParser.Build(byte headerLength, byte protocolVersion, ushort totalLength, byte[] responseBytes)
        {
            return Build(headerLength, protocolVersion, totalLength, responseBytes);
        }

        public TunnelAckResponse Build(byte headerLength, byte protocolVersion, ushort totalLength, byte[] responseBytes)
        {
            var communicationChannel = responseBytes[1];
            var sequenceCounter = responseBytes[2];


            return new TunnelAckResponse(communicationChannel, sequenceCounter, 0x00);
        }
    }
}
