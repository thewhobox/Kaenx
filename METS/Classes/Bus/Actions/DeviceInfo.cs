using METS.Knx.Addresses;
using METS.Knx.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.Classes.Bus.Actions
{
    public class DeviceInfo : IBusAction, INotifyPropertyChanged
    {
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private BusConnection _conn;
        private byte _sequence = 0x00;
        private bool stopCheck = false;
        private DeviceInfoData _data = new DeviceInfoData();

        public string Type { get; } = "Geräte Info";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public event EventHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        private int _state = 0;

        public DeviceInfo()
        {
            _conn = BusConnection.Instance;
            _conn.OnTunnelResponse += _conn_OnTunnelResponse;
        }

        private void _conn_OnTunnelResponse(TunnelResponse response)
        {
            bool sendAck = true;
            int seq = response.SequenceNumber << 2;
            int test2 = 0xC2 | seq;

            byte[] apci = { BitConverter.GetBytes(Convert.ToInt16(test2))[0] };

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
            } else if (_state == 4 && response.APCI == Knx.Parser.ApciTypes.MemoryResponse)
            {
                State4(response);
            } else {
                sendAck = false;
            }


            if (!sendAck) return;
            _sequence++;
            TunnelRequest builder = new TunnelRequest(); //TODO ack wieder gängig machen
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.Ack, _sequence, BitConverter.GetBytes(response.SequenceNumber)[1]);
            _conn.SendAsync(builder);
        }

        public void Run()
        {
            _state = 0;
            TodoText = "Lese Geräteinfo...";
            Start();
        }


        private async void Start()
        {
            TodoText = "Lese Maskenversion...";
            // Connect
            TunnelRequest builder = new TunnelRequest();
            byte[] apci = { 0x80 };
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.Connect, _sequence, 255, null);
            _conn.SendAsync(builder);
            await Task.Delay(100);
            _sequence++;

            // Read Property (MaskVersion)
            builder = new TunnelRequest();
            apci = new byte[] { 0x43, 0x00 };
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.DeviceDescriptorRead, _sequence, 0);
            _conn.SendAsync(builder);
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
            byte[] apci = { 0x47, 0xd5 };
            byte[] data = { 0, 11, 0x01 << 4, 0x01 };
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.PropertyValueRead, _sequence, 1, data);
            _conn.SendAsync(builder);
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
            byte[] apci = { 0x4B, 0xd5 };
            byte[] data = { 3, 13, 0x01 << 4, 0x01 };
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.PropertyValueRead, _sequence, 2, data);
            _conn.SendAsync(builder);
            _state = 3;
        }

        private async void State3(TunnelResponse response) 
        {
            ProgressValue = 30;
            _data.ApplicationId = BitConverter.ToString(response.Data).Replace("-", "").Substring(8);


            TodoText = "Lese Gruppentabelle...";
            await Task.Delay(500);

            _sequence++;
            TunnelRequest builder = new TunnelRequest();
            byte[] address = BitConverter.GetBytes(Convert.ToInt16(16384));
            byte[] apci = { 0x02, 0x00 }; // 0x4B, 0xd5 };

            int _count = 12;
            int _apci = 8 << 6;
            int _tpci = 19 << 10;

            int xy = _count | _apci | _tpci;

            apci = BitConverter.GetBytes(Convert.ToInt16(xy));
            Array.Reverse(apci);
            Array.Reverse(address);

            //TODO check needed changes

            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.MemoryRead, _sequence, 3, address);
            _conn.SendAsync(builder);
            _state = 4;

        }

        private void State4(TunnelResponse response)
        {
            TodoText = "Verarbeite Gruppentabelle...";
            int length = BitConverter.ToUInt16(new byte[] { response.Data[2], 0x00 }, 0);
            UnicastAddress self = UnicastAddress.FromByteArray(new byte[] { response.Data[3], response.Data[4] });

            List<MulticastAddress> addresses = new List<MulticastAddress>();

            for (int i = 0; i < length -1; i++)
            {
                int offset = (i*2) + 5;

                byte mm = response.Data[offset];
                int main = mm >> 3;

                int middle = mm & 7;

                mm = BitConverter.GetBytes((main << 4) | middle)[0];


                addresses.Add(MulticastAddress.FromByteArray(new byte[] { mm, response.Data[offset + 1] }));
            }

            _data.GroupTable = addresses;

            Finish();
        }

        private void Finish()
        {
            _conn.OnTunnelResponse -= _conn_OnTunnelResponse;

            ProgressValue = 100;
            TodoText = "";
            Finished?.Invoke(_data, new EventArgs());
        }

        private void Changed(string name)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            } catch
            {
                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                });
            }
        }
    }
}
