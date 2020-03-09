using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Konnect.Parser
{
    public class DisconnectResponseParser : IReceiveParser
    {
        public ushort ServiceTypeIdentifier => 0x0209;

        IResponse IReceiveParser.Build(byte headerLength, byte protocolVersion, ushort totalLength,
          byte[] responseBytes)
        {
            return Build(headerLength, protocolVersion, totalLength, responseBytes);
        }

        public DisconnectResponse Build(byte headerLength, byte protocolVersion, ushort totalLength, byte[] responseBytes)
        {
            var communicationChannel = responseBytes[0];
            var status = responseBytes[1];

            return new DisconnectResponse(headerLength, protocolVersion, totalLength, communicationChannel, status);
        }
    }
}
