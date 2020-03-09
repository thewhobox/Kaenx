using Kaenx.Konnect;
using Kaenx.Konnect.Classes;
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

        public Connection Connection { get; set; }

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
            dev.Connect();
            await Task.Delay(50);

            for (int i = 1; i < 6; i++)
            {
                byte[] data = new byte[11];
                int state = (int)LoadStateMachineState.Unloaded;
                int end = (i << 4) | state;
                data[0] = Convert.ToByte(end);
                await dev.MemoryWriteSync(260, data);
                await Task.Delay(50);
                data = await dev.MemoryRead(46825 + i, 1);
                Debug.WriteLine(i + ": " + BitConverter.ToString(data).Replace("-", ""));
            }

            TodoText = "Gerät neu starten...";
            await Task.Delay(500);
            dev.Restart();

            Device.IsDeactivated = !Device.IsDeactivated;

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


        private enum LoadStateMachineState
        {
            Undefined,
            Loading,
            Loaded,
            Error,
            Unloaded
        }
    }
}
