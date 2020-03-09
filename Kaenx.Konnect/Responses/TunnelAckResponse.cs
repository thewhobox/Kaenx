using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Konnect.Responses
{
    public class TunnelAckResponse : IResponse
    {
        public TunnelAckResponse(byte channel, byte sequenz, byte status)
        {
            ChannelId = channel;
            SequenceCounter = sequenz;
            Status = status;
        }

        public byte ChannelId { get; }
        public byte SequenceCounter { get; }
        public byte Status { get; }
    }
}
