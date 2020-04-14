using Kaenx.DataContext.Catalog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus.Data
{
    public class NewDeviceData : INotifyPropertyChanged
    {
        private string _status = "Wartend...";
        private string _manu;
        private string _name;

        public byte[] Serial { get; set; }
        public string SerialText { get { return BitConverter.ToString(Serial).Replace("-", ""); } }
        public string Status
        {
            get { return _status; }
            set { _status = value; Changed("Status"); }
        }
        public string Manufacturer
        {
            get { return _manu; }
            set { _manu = value; Changed("Manufacturer"); }
        }
        public string DeviceName
        {
            get { return _name; }
            set { _name = value; Changed("DeviceName"); }
        }
        public string ApplicationId { get; set; }
        public bool finished { get; set; } = false;

        public List<DeviceViewModel> DeviceModels { get; set; } = new List<DeviceViewModel>();


        public event PropertyChangedEventHandler PropertyChanged;

        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
