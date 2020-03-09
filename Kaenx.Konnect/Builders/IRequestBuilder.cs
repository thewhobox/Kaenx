using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Kaenx.Konnect.Builders
{
    public interface IRequestBuilder
    {
        byte[] GetBytes();
        void SetChannelId(byte channelId);
        void SetSequence(byte sequence);
    }
}
