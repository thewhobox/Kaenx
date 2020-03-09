using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Konnect.Classes
{
    public class BusCommon
    {
        private Connection _conn;
        private MulticastAddress to = MulticastAddress.FromString("0/0/0");
        private UnicastAddress from = UnicastAddress.FromString("0.0.0");
        private Dictionary<byte, TunnelResponse> responses = new Dictionary<byte, TunnelResponse>();

        public BusCommon(Connection conn)
        {
            _conn = conn;
            _conn.OnTunnelRequest += OnTunnelRequest;
        }

        private void OnTunnelRequest(TunnelResponse response)
        {
            responses.Add(response.SequenceCounter, response);
            //TODO move ack to connection class!
            //TunnelRequest builder = new TunnelRequest();
            //builder.Build(UnicastAddress.FromString("0.0.0"), from, Parser.ApciTypes.Ack, Convert.ToByte(response.SequenceNumber));
            //_conn.Send(builder);
        }



        private async Task<TunnelResponse> WaitForData(byte seq)
        {
            while (!responses.ContainsKey(seq))
                await Task.Delay(10); // TODO maybe erhöhen

            var resp = responses[seq];
            responses.Remove(seq);
            return resp;
        }



        public void IndividualAddressRead()
        {
            TunnelRequest builder = new TunnelRequest();
            builder.Build(from, to, Parser.ApciTypes.IndividualAddressRead);
            _conn.Send(builder);
        }

        public void IndividualAddressWrite(UnicastAddress newAddr)
        {
            TunnelRequest builder = new TunnelRequest();
            builder.Build(MulticastAddress.FromString("0/0/0"), MulticastAddress.FromString("0/0/0"), Parser.ApciTypes.IndividualAddressWrite, 255, newAddr.GetBytes());
            builder.SetPriority(Prios.System);
            _conn.Send(builder);
        }




        public void GroupValueWrite(MulticastAddress ga, byte[] data)
        {
            TunnelRequest builder = new TunnelRequest();
            builder.Build(from, ga, Parser.ApciTypes.GroupValueWrite, 255, data);
            _conn.Send(builder);
        }

        public async Task GroupValueRead(MulticastAddress ga)
        {
            TunnelRequest builder = new TunnelRequest();
            builder.Build(from, ga, Parser.ApciTypes.GroupValueWrite);
            var seq = _conn.Send(builder);
        }
    }
}
