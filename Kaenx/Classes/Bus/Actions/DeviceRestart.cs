using Kaenx.Classes.Project;
using Kaenx.Konnect;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Connections;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Kaenx.Classes.Bus.Actions.IBusAction;

namespace Kaenx.Classes.Bus.Actions
{
    public class DeviceRestart : IBusAction, INotifyPropertyChanged
    {
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private CancellationToken _token;

        public string Type { get; } = "Geräte Neustarten";
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
            TodoText = "Gerät neu starten...";

            Start();
        }


        private async void Start()
        {
            BusDevice dev = new BusDevice(Device.LineName, Connection);
            await dev.Connect(true);
            dev.Restart();
            await Task.Delay(2000);
            Analytics.TrackEvent("Gerät neu gestartet");
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
