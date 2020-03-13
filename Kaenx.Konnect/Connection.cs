using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Addresses;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Responses;
using Kaenx.Konnect.Parser;
using System.Net.NetworkInformation;
using System.Linq;
using System.Diagnostics;

namespace Kaenx.Konnect
{
    public class Connection
    {
        public delegate void ConnectionChangedHandler(bool isConnected);
        public event ConnectionChangedHandler ConnectionChanged;

        public delegate void TunnelRequestHandler(Builders.TunnelResponse response);
        public event TunnelRequestHandler OnTunnelRequest;
        public event TunnelRequestHandler OnTunnelResponse;
        public event TunnelRequestHandler OnTunnelAck;

        private readonly IPEndPoint _receiveEndPoint;
        private readonly IPEndPoint _sendEndPoint;
        private UdpClient _udpClient;
        private readonly BlockingCollection<byte[]> _sendMessages;
        private readonly ReceiverParserDispatcher _receiveParserDispatcher;
        private byte _communicationChannel;
        private byte _sequenceCounter = 0;

        public bool IsConnected { get; private set; }


        public Connection(IPEndPoint sendEndPoint)
        {
            _sendEndPoint = sendEndPoint;
            _receiveEndPoint = new IPEndPoint(IPAddress.Any, GetFreePort());
            _udpClient = new UdpClient(_receiveEndPoint);
            _receiveParserDispatcher = new ReceiverParserDispatcher();
            _sendMessages = new BlockingCollection<byte[]>();

            ProcessSendMessages();
        }

        public int GetFreePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }



        public void Connect()
        {
            ConnectionRequest builder = new ConnectionRequest();
            builder.Build(_receiveEndPoint, 0x00);
            _sendMessages.Add(builder.GetBytes());
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            DisconnectRequest builder = new DisconnectRequest();
            builder.Build(_receiveEndPoint, _communicationChannel);
            _sendMessages.Add(builder.GetBytes());
        }

        /// <summary>
        /// Sendet die Daten vom angegebenen Builder.
        /// </summary>
        /// <param name="builder">Builder</param>
        /// <returns>Gibt den Sequenz Counter zurück</returns>
        public byte Send(IRequestBuilder builder)
        {
            if (!IsConnected)
                throw new Exception("Roflkopter");

            var seq = _sequenceCounter;
            builder.SetChannelId(_communicationChannel);
            builder.SetSequence(_sequenceCounter);
            _sequenceCounter++;
            byte[] data = builder.GetBytes();

            _sendMessages.Add(data);

            return seq;
        }

        /// <summary>
        /// Sendet die Daten vom angegebenen Builder.
        /// </summary>
        /// <param name="builder">Builder</param>
        /// <returns>Gibt den Sequenz Counter zurück</returns>
        public async Task<byte> SendAsync(IRequestBuilder builder)
        {
            if (!IsConnected)
                throw new Exception("Roflkopter");

            var seq = _sequenceCounter;
            builder.SetChannelId(_communicationChannel);
            builder.SetSequence(_sequenceCounter);
            _sequenceCounter++;
            byte[] data = builder.GetBytes();

            _sendMessages.Add(data);

            return seq;
        }

        public Task SendAsync(byte[] bytes)
        {
            if (!IsConnected)
                throw new Exception("Roflkopter");

            _sendMessages.Add(bytes);

            return Task.CompletedTask;
        }



        private void ProcessSendMessages()
        {
            Task.Run(async () =>
            {
                int rofl = 0;
                try
                {

                    while (true)
                    {
                        rofl++;
                        var result = await _udpClient.ReceiveAsync();
                        var knxResponse = _receiveParserDispatcher.Build(result.Buffer);
                        switch (knxResponse)
                        {
                            case ConnectResponse connectResponse:
                                if (connectResponse.Status == 0x00)
                                {
                                    _sequenceCounter = 0;
                                    _communicationChannel = connectResponse.CommunicationChannel;
                                    IsConnected = true;
                                    ConnectionChanged?.Invoke(IsConnected);
                                } else
                                {

                                }

                                break;
                            case Builders.TunnelResponse tunnelResponse:
                                _sendMessages.Add(new Responses.TunnelResponse(0x06, 0x10, 0x0A, 0x04, _communicationChannel, tunnelResponse.SequenceCounter, 0x00).GetBytes());


                                if (tunnelResponse.APCI.ToString().EndsWith("Response"))
                                {
                                    TunnelRequest builder = new TunnelRequest();
                                    builder.Build(UnicastAddress.FromString("0.0.0"), tunnelResponse.SourceAddress, Parser.ApciTypes.Ack, tunnelResponse.SequenceNumber);
                                    Send(builder);
                                    OnTunnelResponse?.Invoke(tunnelResponse);
                                }
                                else if (tunnelResponse.APCI == ApciTypes.Ack)
                                {
                                    OnTunnelAck?.Invoke(tunnelResponse);
                                }
                                else
                                {
                                    OnTunnelRequest?.Invoke(tunnelResponse);
                                }


                                break;

                            case TunnelAckResponse tunnelAck:
                                
                                break;

                            case DisconnectResponse disconnectResponse:
                                IsConnected = false;
                                _communicationChannel = 0;
                                ConnectionChanged?.Invoke(IsConnected);
                                break;
                        }
                    }
                }catch
                {

                }
            });

            Task.Run(() =>
            {
                
                foreach (var sendMessage in _sendMessages.GetConsumingEnumerable())
                    _udpClient.SendAsync(sendMessage, sendMessage.Length, _sendEndPoint);
            });
        }


    }
}
