using Kaenx.Classes.Buildings;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.Konnect;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Connections;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using static Kaenx.Classes.Bus.Actions.IBusAction;

namespace Kaenx.Classes.Bus.Actions
{
    public class DeviceDeactivate : IBusAction, INotifyPropertyChanged
    {
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private CancellationToken _token;

        public string Type { get; } = "Geräte De-/Aktivieren";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public IKnxConnection Connection { get; set; }

        public event ActionFinishedHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;


        public void Run(CancellationToken token)
        {
            _token = token; // TODO implement cancellation
            TodoText = Device.IsDeactivated ? "Gerät aktivieren..." : "Gerät deaktiveren...";

            Start();
        }

        private async void Start()
        {
            BusDevice dev = new BusDevice(Device.LineName, Connection);
            await dev.Connect();

            if (Device.IsDeactivated)
            {
                await dev.RessourceWrite("GroupAddressTable", new byte[] { (byte)Device.LastGroupCount });
            } else
            {
                int size = await dev.RessourceRead<int>("GroupAddressTable");
                Device.LastGroupCount = size;
                await dev.RessourceWrite("GroupAddressTable", new byte[] { 0x01 });
            }



            TodoText = "Gerät neu starten...";
            await dev.Restart();

            Device.IsDeactivated = !Device.IsDeactivated;

            Analytics.TrackEvent("Gerät de-/aktivieren");
            Finished?.Invoke(this, null);
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
