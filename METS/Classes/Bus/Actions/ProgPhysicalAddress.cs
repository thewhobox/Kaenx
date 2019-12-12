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
    public class ProgPhysicalAddress : IBusAction, INotifyPropertyChanged
    {
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private BusConnection _conn;
        private byte _sequence = 0x00;
        private List<string> progDevices = new List<string>();
        private CancellationToken _token;

        public string Type { get; } = "Physikalische Adresse";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public event EventHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        int sCounter = 0;

        public ProgPhysicalAddress(BusConnection connection)
        {
            _conn = connection;
            _conn.OnTunnelResponse += _conn_OnTunnelResponse;
        }

        List<TunnelResponse> devices = new List<TunnelResponse>();

        private void _conn_OnTunnelResponse(TunnelResponse response)
        {
            devices.Add(response);

            //switch(response.MessageCode)
            //{
            //    case 0x29:
            //        if(response.SourceAddress.Area != 0 && !progDevices.Contains(response.SourceAddress.ToString()))
            //            progDevices.Add(response.SourceAddress.ToString());
            //        break;
            //}

            if(response.APCI == Knx.Parser.ApciTypes.IndividualAddressResponse && response.SourceAddress.Area != 0 && !progDevices.Contains(response.SourceAddress.ToString()))
            {
                progDevices.Add(response.SourceAddress.ToString());
            }
        }

        public void Run(CancellationToken token)
        {
            _token = token; //TODO implement cancellation
            CheckProgMode();
        }

        private async void CheckProgMode()
        {
            TodoText = "Bitte Programmierknopf drücken...";
            ProgressIsIndeterminate = true;
            progDevices.Clear();

            int threeshold = 0;

            while(!_token.IsCancellationRequested)
            {
                if (!_conn.IsConnected)
                {
                    _conn.Connect();
                    await Task.Delay(2000);
                    continue;
                }
                if (progDevices.Count == 1)
                {
                    if(progDevices[0] == Device.LineName)
                    {
                        TodoText = "Gerät hat bereits die Adresse";
                        Device.LoadedPA = true;
                        Finished?.Invoke(this, new EventArgs());
                        break;
                    }
                    else
                    {
                        SetAddress();
                    }
                    break;
                } else if(progDevices.Count > 1)
                {
                    TodoText = "Es befinden sich mehrere Geräte im Programmiermodus";
                    if(threeshold > 5)
                    {
                        await Task.Delay(3000);
                        Finished?.Invoke(this, new EventArgs());
                        break;
                    }
                    threeshold++;
                    continue;
                }
                progDevices.Clear();
                TunnelRequest builder = new TunnelRequest();
                builder.Build(UnicastAddress.FromString("0.0.0"), MulticastAddress.FromString("0/0/0"), Knx.Parser.ApciTypes.IndividualAddressRead, _sequence, 0);
                _conn.SendAsync(builder);
                _sequence++;

                await Task.Delay(500);
            }
        }

        private async void SetAddress()
        {
            TodoText = "Adresse wird programmiert";
            ProgressIsIndeterminate = false;
            ProgressValue = 33;
            UnicastAddress newAddr = UnicastAddress.FromString(Device.LineName);



            TunnelRequest builder = new TunnelRequest();
            byte[] apci = { 0x00, 0xc0 };
            builder.Build(MulticastAddress.FromString("0/0/0"), MulticastAddress.FromString("0/0/0"), Knx.Parser.ApciTypes.IndividualAddressWrite, _sequence, 255, newAddr.GetBytes());
            builder.SetPriority(Prios.System);
            _conn.SendAsync(builder);
            _sequence++;

            await Task.Delay(500);

            CheckNewAddr();
        }

        private async void CheckNewAddr()
        {
            TodoText = "Adresse wird geprüft";
            ProgressValue = 66;
            progDevices.Clear();

            _conn.IncreaseSequence();

            while (!_token.IsCancellationRequested)
            {
                if (progDevices.Count == 1)
                {
                    if(progDevices[0] == Device.LineName)
                    {
                        RestartDevice();
                    } else
                    {
                        TodoText = "Adresse konnte nicht programmiert werden";
                        await Task.Delay(3000);
                        Finished?.Invoke(this, new EventArgs());
                        break;
                    }
                    break;
                }
                else if (progDevices.Count > 1)
                {
                    TodoText = "Es befinden sich mehrere Geräte im Programmiermodus"; 
                    await Task.Delay(3000);
                    Finished?.Invoke(this, new EventArgs());
                    break;
                }
                progDevices.Clear();
                TunnelRequest builder = new TunnelRequest();
                byte[] apci = { 0x01, 0x00 };
                builder.Build(UnicastAddress.FromString("0.0.0"), MulticastAddress.FromString("0/0/0"), Knx.Parser.ApciTypes.IndividualAddressRead, _sequence, 255);
                _conn.SendAsync(builder);
                _sequence++;
                await Task.Delay(500);
            }
        }

        private async void RestartDevice()
        {
            TodoText = "Gerät wird neu gestartet";
            ProgressValue = 95;


            TunnelRequest builder = new TunnelRequest();
            byte[] apci = { 0x80 };
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.Connect, _sequence, 255);
            _conn.SendAsync(builder);
            _sequence++;
            await Task.Delay(200);

            builder = new TunnelRequest();
            apci = new byte[] { 0x43, 0x80 };
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.Restart, _sequence, 0);
            _conn.SendAsync(builder);
            _sequence++;
            await Task.Delay(200);

            builder = new TunnelRequest();
            apci = new byte[] { 0x81 };
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.IndividualAddressRead, _sequence, 2);
            _conn.SendAsync(builder);
            _sequence++;
            ProgressValue = 100;
            TodoText = "Abgeschlossen";
            await Task.Delay(2000);

            Device.LoadedPA = true;

            Finished?.Invoke(this, new EventArgs());
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
