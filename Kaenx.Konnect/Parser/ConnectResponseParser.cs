using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Kaenx.Konnect.Parser
{
    public class ConnectResponseParser : IReceiveParser
    {
        public ushort ServiceTypeIdentifier => 0x0206;

        IResponse IReceiveParser.Build(byte headerLength, byte protocolVersion, ushort totalLength,
          byte[] responseBytes)
        {
            return Build(headerLength, protocolVersion, totalLength, responseBytes);
        }

        public ConnectResponse Build(byte headerLength, byte protocolVersion, ushort totalLength, byte[] responseBytes)
        {
            var communicationChannel = responseBytes[0];
            var status = responseBytes[1];
            var dataEndpoint = ParseEndpoint(responseBytes.Skip(2).Take(8));
            var connectionResponseDataBlock = ParseConnectionResponseDataBlock(responseBytes.Skip(10).Take(4));

            return new ConnectResponse(headerLength, protocolVersion, totalLength, communicationChannel, status, dataEndpoint,
              connectionResponseDataBlock);
        }

        private static ConnectionResponseDataBlock ParseConnectionResponseDataBlock(IEnumerable<byte> bytes)
        {
            var enumerable = bytes as byte[] ?? bytes.ToArray();
            return new ConnectionResponseDataBlock(enumerable.ElementAt(0), enumerable.ElementAt(1),
              UnicastAddress.FromByteArray(enumerable.Skip(2).ToArray()));
        }

        private static HostProtocolAddressInformation ParseEndpoint(IEnumerable<byte> bytes)
        {
            var enumerable = bytes as byte[] ?? bytes.ToArray();
            return new HostProtocolAddressInformation(enumerable.ElementAt(1),
              new IPEndPoint(BitConverter.ToInt32(enumerable.Skip(2).Take(4).ToArray(), 0),
                BitConverter.ToInt16(enumerable.Skip(6).Take(2).Reverse().ToArray(), 0)));
        }
    }
}
