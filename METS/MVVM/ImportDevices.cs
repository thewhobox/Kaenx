using METS.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.MVVM
{
    public class ImportDevices : INotifyPropertyChanged, IDisposable
    {
        private ObservableCollection<Device> deviceList = new ObservableCollection<Device>();
        public ObservableCollection<Device> DeviceList
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

        private ObservableCollection<string> languages = new ObservableCollection<string>();
        public ObservableCollection<string> Languages
        {
            get
            {
                return languages;
            }
            set
            {
                languages = value;
                Update("Languages");
            }
        }

        private string selectedLang = "";
        public string SelectedLanguage
        {
            get
            {
                return selectedLang;
            }
            set
            {
                selectedLang = value;
                Update("SelectedLanguage");
            }
        }

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
