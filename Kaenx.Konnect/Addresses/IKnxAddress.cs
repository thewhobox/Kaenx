using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Konnect.Addresses
{
    public interface IKnxAddress
    {
        byte[] GetBytes();
    }
}
