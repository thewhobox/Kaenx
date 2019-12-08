using METS.Knx.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace METS.Knx.Classes
{
    public interface IReceiveParser
    {
        ushort ServiceTypeIdentifier { get; }
        IResponse Build(byte headerLength, byte protocolVersion, ushort totalLength, byte[] responseBytes);
    }
}
