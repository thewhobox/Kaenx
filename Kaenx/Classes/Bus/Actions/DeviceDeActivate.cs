using Kaenx.DataContext.Catalog;
using Kaenx.Konnect;
using Kaenx.Konnect.Classes;
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

            CatalogContext _context = new CatalogContext();
            ApplicationViewModel app = _context.Applications.Single(a => a.Id == Device.ApplicationId);


            XDocument master = await GetKnxMaster();
            XElement mask = master.Descendants(XName.Get("MaskVersion", master.Root.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == app.Mask);
            XElement procedure = mask.Descendants(XName.Get("Procedure", master.Root.Name.NamespaceName)).Single(m => m.Attribute("ProcedureType").Value == "Unload");

            foreach (XElement ele in procedure.Elements())
            {
                if (ele.Name.LocalName != "LdCtrlUnload") continue;

                int lsmIdx = int.Parse(ele.Attribute("LsmIdx").Value);
                byte[] data = new byte[11];
                int state = Device.IsDeactivated ? (int)LoadStateMachineState.Loaded : (int)LoadStateMachineState.Unloaded;
                int end = (lsmIdx << 4) | state;
                data[0] = Convert.ToByte(end);
                await dev.MemoryWriteSync(260, data);
                await Task.Delay(50);
                data = await dev.MemoryRead(46825 + lsmIdx, 1);
                Debug.WriteLine(lsmIdx + ": " + BitConverter.ToString(data).Replace("-", ""));
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



        private async Task<XDocument> GetKnxMaster()
        {
            StorageFile masterFile;

            try
            {
                masterFile = await ApplicationData.Current.LocalFolder.GetFileAsync("knx_master.xml");
            }
            catch
            {
                StorageFile defaultFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/knx_master.xml"));
                masterFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("knx_master.xml");
                await FileIO.WriteTextAsync(masterFile, await FileIO.ReadTextAsync(defaultFile));
            }


            XDocument masterXml = XDocument.Load(await masterFile.OpenStreamForReadAsync());
            return masterXml;
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
