using System;
using System.Collections.Generic;
using System.Text;

namespace METS.Knx.Addresses
{
    public interface IKnxAddress
    {
        byte[] GetBytes();
    }
}
