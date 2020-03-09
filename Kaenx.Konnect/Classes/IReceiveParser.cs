using Kaenx.Konnect.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Konnect.Classes
{
    public interface IReceiveParser
    {
        ushort ServiceTypeIdentifier { get; }
        IResponse Build(byte headerLength, byte protocolVersion, ushort totalLength, byte[] responseBytes);
    }
}
