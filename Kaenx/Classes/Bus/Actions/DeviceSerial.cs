using Kaenx.Classes.Bus.Data;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Connections;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class DeviceSerial : IBusAction, INotifyPropertyChanged
    {
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private CancellationToken _token;

        public string Type { get; } = "Geräte Seriennr.";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public IKnxConnection Connection { get; set; }
        IKnxConnection IBusAction.Connection { get; set; }

        public event ActionFinishedHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        public DeviceSerial()
        { 
        }

        public void Run(CancellationToken token)
        {
            _token = token; // TODO implement cancellation
            TodoText = "Lese Seriennummer...";

            Start();
        }

        private async void Start()
        {
            try
            {
                BusDevice dev = new BusDevice(Device.LineName, Connection);
                await dev.Connect(true);

                byte[] number = await dev.PropertyRead(0, 11);

                _= App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Device.Serial = number;
                    Device.LoadedPA = true;
                    SaveHelper.UpdateDevice(Device);
                });
            } catch(OperationCanceledException)
            {
                Finish("Gerät antwortet nicht");
                return;
            }catch(Exception e)
            {
                Finish(e.Message +  Environment.NewLine + e.StackTrace);
                return;
            }

            Finish();
        }


        private void Finish(string text = "")
        {
            ProgressValue = 100;
            Analytics.TrackEvent("Geräte Serial auslesen");

            if (string.IsNullOrEmpty(text)) TodoText = "Erfolgreich";
            else TodoText = text;

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
    }
}
