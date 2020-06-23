using Kaenx.Classes.Bus.Data;
using Kaenx.Classes.Dynamic;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Classes;
using Microsoft.AppCenter.Analytics;
using Serilog;
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

namespace Kaenx.Classes.Bus.Actions
{
    public class DeviceMem : IBusAction, INotifyPropertyChanged
    {
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private DeviceConfigData _data = new DeviceConfigData();
        private CancellationToken _token;
        private BusDevice dev;

        public string Type { get; } = "Geräte Speicher";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public Connection Connection { get; set; }

        public event IBusAction.ActionFinishedHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        public DeviceMem()
        { 
        }

        public void Run(CancellationToken token)
        {
            _token = token; // TODO implement cancellation
            TodoText = "Verbinden...";

            Start();
        }

        private async void Start()
        {
            dev = new BusDevice(Device.LineName, Connection);
            await dev.Connect();

            CatalogContext context = new CatalogContext();
            ApplicationViewModel appModel = null;

            if (context.Applications.Any(a => a.Id == Device.ApplicationId))
            {
                appModel = context.Applications.Single(a => a.Id == Device.ApplicationId);
            }

            if(appModel != null)
            {
                if (!string.IsNullOrEmpty(appModel.Table_Assosiations))
                {
                    AppSegmentViewModel segmentModel = context.AppSegments.Single(s => s.Id == "M-0083_A-0023-15_AS-4400");
                    byte[] datax = await dev.MemoryRead(segmentModel.Address, segmentModel.Size);
                }
            }



            Finish();
        }


        private void Finish(string errmsg = null)
        {
            ProgressValue = 100;
            Finished?.Invoke(this, _data);
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
