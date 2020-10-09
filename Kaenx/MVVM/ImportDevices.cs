using Kaenx.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.MVVM
{
    public class ImportDevices : INotifyPropertyChanged, IDisposable
    {
        private ObservableCollection<Kaenx.Classes.Device> deviceList = new ObservableCollection<Kaenx.Classes.Device>();
        public ObservableCollection<Kaenx.Classes.Device> DeviceList
        {
            get
            {
                return deviceList;
            }
            set
            {
                deviceList = value;
                Update("DeviceList");
            }
        }

        public string SelectedLanguage { get; set; }
        public bool wasFromMain = false;

        public ZipArchive Archive { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Update(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            DeviceList = null;
            Archive.Dispose();
        }
    }
}
