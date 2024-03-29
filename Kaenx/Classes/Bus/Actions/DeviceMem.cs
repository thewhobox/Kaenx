﻿using Kaenx.Classes.Bus.Data;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Connections;
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

        public IKnxConnection Connection { get; set; }

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

            TodoText = "Lege los";

            CatalogContext context = new CatalogContext();
            ApplicationViewModel appModel = null;

            //TODO check change
            if (context.Applications.Any(a => true)) //a.Id == Device.ApplicationId))
            {
                appModel = context.Applications.Single(a => true); // a.Id == Device.ApplicationId);
            }

            Dictionary<int, byte[]> segments = new Dictionary<int, byte[]>();

            foreach(AppSegmentViewModel seg in context.AppSegments.Where(s => s.ApplicationId == Device.ApplicationId))
            {
                byte[] datax = await dev.MemoryRead(seg.Address, seg.Size);
                segments.Add(seg.Id, datax);
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
