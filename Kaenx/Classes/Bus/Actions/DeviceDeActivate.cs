using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.Konnect;
using Kaenx.Konnect.Classes;
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


            CatalogContext context = new CatalogContext();
            ApplicationViewModel app = context.Applications.Single(a => a.Id == Device.ApplicationId);
            AppSegmentViewModel seg = context.AppAbsoluteSegments.Single(s => s.Id == app.Table_Group);
            int addr = seg.Address;

            if (Device.IsDeactivated)
            {
                //Alle verbundenen GAs finden und sortieren
                List<string> addedGroups = new List<string> { "" };
                foreach (DeviceComObject com in Device.ComObjects)
                    foreach (GroupAddress group in com.Groups)
                        if (!addedGroups.Contains(group.GroupName))
                            addedGroups.Add(group.GroupName);

                addedGroups.Sort();

                //länge der Tabelle erstmal auf 1 setzen
                await dev.MemoryWriteSync(addr, new byte[] { Convert.ToByte(addedGroups.Count) });
            } else
            {
                //länge der Tabelle erstmal auf 1 setzen
                await dev.MemoryWriteSync(addr, new byte[] { 0x01 });
            }


            TodoText = "Gerät neu starten...";
            dev.Restart();

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
