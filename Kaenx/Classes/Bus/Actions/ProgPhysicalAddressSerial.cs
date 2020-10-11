using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Connections;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Kaenx.Classes.Bus.Actions.IBusAction;

namespace Kaenx.Classes.Bus.Actions
{
    public class ProgPhysicalAddressSerial : IBusAction, INotifyPropertyChanged
    {
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private List<string> progDevices = new List<string>();
        private CancellationToken _token;
        private BusCommon bus;

        public string Type { get; } = "Physikalische Adresse";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public IKnxConnection Connection { get; set; }

        public event ActionFinishedHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        public ProgPhysicalAddressSerial()
        {
        }

        public void Run(CancellationToken token)
        {
            _token = token;
            bus = new BusCommon(Connection);

            TodoText = "Neue Ph. Adresse wird geschrieben...";

            Start();
        }

        private async void Start()
        {

            await Task.Delay(100);

            bus.IndividualAddressWrite(UnicastAddress.FromString(Device.LineName), Device.Serial);

            await Task.Delay(100);
            await RestartCommands();

            await Task.Delay(2000);
            BusDevice dev = new BusDevice(Device.LineName, Connection);
            await dev.Connect(true);
            byte[] serial = await dev.PropertyRead(0, 11);

            if(Device.SerialText != BitConverter.ToString(serial).Replace("-", ""))
            {
                TodoText = "Fehlgeschlagen";
            } else
            {
                TodoText = "Erfolgreich abgeschlossen";

                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    Device.LoadedPA = true;
                });
            }

            Analytics.TrackEvent("Prog Addr Serial");
            Finished?.Invoke(this, null);
        }

        private async Task RestartCommands()
        {
            TodoText = "Gerät wird neu gestartet";
            BusDevice dev = new BusDevice(Device.LineName, Connection);
            await dev.Connect(true);
            dev.Restart();
            await Task.Delay(1000);
            ProgressValue = 100;
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
