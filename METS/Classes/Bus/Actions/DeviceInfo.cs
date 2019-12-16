using METS.Context.Catalog;
using METS.Knx;
using METS.Knx.Addresses;
using METS.Knx.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace METS.Classes.Bus.Actions
{
    public class DeviceInfo : IBusAction, INotifyPropertyChanged
    {
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private byte _sequence = 0x00;
        private DeviceInfoData _data = new DeviceInfoData();
        private CancellationToken _token;

        public string Type { get; } = "Geräte Info";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public Connection Connection { get; set; }

        public event EventHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        private int _state = 0;
        private int GroupAddress;

        public DeviceInfo()
        { 
        }

        private void Conn_OnTunnelResponse(TunnelResponse response)
        {
            bool sendAck = true;

            if (_state == 1 && response.APCI == Knx.Parser.ApciTypes.DeviceDescriptorResponse)
            {
                State1(response);
            }
            else if (_state == 2 && response.APCI == Knx.Parser.ApciTypes.PropertyValueResponse)
            {
                State2(response);
            } else if (_state == 3 && response.APCI == Knx.Parser.ApciTypes.PropertyValueResponse)
            {
                State3(response);
            }
            else if (_state == 4 && response.APCI == Knx.Parser.ApciTypes.MemoryResponse)
            {
                State4(response);
            } else if (_state == 5 && response.APCI == Knx.Parser.ApciTypes.MemoryResponse)
            {
                State5(response);
            }
            else {
                sendAck = false;
            }

            if (!sendAck) return;
            _sequence++;
            TunnelRequest builder = new TunnelRequest(); //TODO ack wieder gängig machen
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.Ack, _sequence, BitConverter.GetBytes(response.SequenceNumber)[0]);
            Connection.Send(builder);
        }

        public void Run(CancellationToken token)
        {
            Connection.OnTunnelRequest += Conn_OnTunnelResponse;
            _token = token; // TODO implement cancellation
            _state = 0;
            TodoText = "Lese Geräteinfo...";
            Start();
        }


        private async void Start()
        {
            TodoText = "Lese Maskenversion...";
            // Connect
            TunnelRequest builder = new TunnelRequest();
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.Connect, _sequence, 255, null);
            Connection.Send(builder);
            await Task.Delay(100);
            _sequence++;

            // Read Property (MaskVersion)
            builder = new TunnelRequest();
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.DeviceDescriptorRead, _sequence, 0);
            Connection.Send(builder);
            _state = 1;
        }

        // 
        private async void State1(TunnelResponse response)
        {
            ProgressValue = 10;
            _data.MaskVersion = BitConverter.ToString(response.Data).Replace("-", "");

            TodoText = "Lese Seriennummer...";
            await Task.Delay(500);
            _sequence++;
            TunnelRequest builder = new TunnelRequest();
            byte[] data = { 0, 11, 0x01 << 4, 0x01 };
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.PropertyValueRead, _sequence, 1, data);
            Connection.Send(builder);
            _state = 2;
        }

        private async void State2(TunnelResponse response)
        {
            ProgressValue = 20;
            _data.SerialNumber = BitConverter.ToString(response.Data).Replace("-", "").Substring(8);

            TodoText = "Lese Applikations Id...";
            await Task.Delay(500);

            _sequence++;
            TunnelRequest builder = new TunnelRequest();
            byte[] data = { 3, 13, 0x01 << 4, 0x01 };
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.PropertyValueRead, _sequence, 2, data);
            Connection.Send(builder);
            _state = 3;
        }

        private async void State3(TunnelResponse response) 
        {
            ProgressValue = 30;
            string appId = BitConverter.ToString(response.Data).Replace("-", "").Substring(8);
            appId = "M-" + appId.Substring(0, 4) + "_A-" + appId.Substring(4, 4) + "-" + appId.Substring(8, 2) + "-";
            _data.ApplicationId = appId + "XXXX";

            TodoText = "Lese Gruppentabelle...";
            await Task.Delay(500);

            _sequence++;
            TunnelRequest builder = new TunnelRequest();
            List<byte> data = new List<byte> { 9 };


            CatalogContext context = new CatalogContext();
            ApplicationViewModel appModel = context.Applications.Single(a => a.Id.StartsWith(appId));

            if (appModel.Table_Group != "" || appModel.Table_Group != null)
            {
                AppAbsoluteSegmentViewModel segmentModel = context.AppAbsoluteSegments.Single(s => s.Id == appModel.Table_Group);
                GroupAddress = segmentModel.Address;
            } else
            {
                //TODO hinzufügen von Adresse aus der Maske holen!
            }


            byte[] address = BitConverter.GetBytes(Convert.ToInt16(GroupAddress));
            Array.Reverse(address);
            data.AddRange(address);

            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.MemoryRead, _sequence, 3, data.ToArray());
            Connection.Send(builder);
            _state = 4;

        }

        private void State4(TunnelResponse response)
        {
            TodoText = "Verarbeite Gruppentabelle...";
            int length = BitConverter.ToUInt16(new byte[] { response.Data[2], 0x00 }, 0);

            if (length <= 1)
            {
                Finish();
                return;
            }



            _sequence++;
            TunnelRequest builder = new TunnelRequest();
            List<byte> data = new List<byte>();

            data.Add(BitConverter.GetBytes((length - 1)*2)[0]);

            byte[] address = BitConverter.GetBytes(Convert.ToInt16(GroupAddress + 3));
            Array.Reverse(address);
            data.AddRange(address);

            //TODO check needed changes

            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.MemoryRead, _sequence, 4, data.ToArray());
            Connection.Send(builder);
            _state = 5;
        }

        private void State5(TunnelResponse response)
        {
            List<MulticastAddress> addresses = new List<MulticastAddress>();

            for (int i = 0; i < ((response.Data.Length-2) / 2); i++)
            {
                int offset = (i*2)+2;
                addresses.Add(MulticastAddress.FromByteArray(new byte[] { response.Data[offset], response.Data[offset + 1] }));
            }

            _data.GroupTable = addresses;

            Finish();
        }

        private void Finish()
        {
            Connection.OnTunnelRequest -= Conn_OnTunnelResponse;

            ProgressValue = 100;
            TodoText = "Erfolgreich";
            Finished?.Invoke(_data, new EventArgs());
        }

        private void Changed(string name)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            catch
            {
                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                });
            }
        }
    }
}
