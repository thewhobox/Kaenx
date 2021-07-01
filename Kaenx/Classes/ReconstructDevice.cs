using Kaenx.Classes.Project;
using Kaenx.Konnect.Addresses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes
{
    public class ReconstructDevice : INotifyPropertyChanged
    {
        public UnicastAddress Address { get; set; }


        public int StateId { get; set; } = 0;
        public bool CanRead { get; set; } = false;


        private string _status;
        public string Status
        {
            get { return _status; }
            set { _status = value; Changed("Status"); }
        }

        public byte[] SerialBytes;

        private string _serail;
        public string Serial
        {
            get { return _serail; }
            set { _serail = value; Changed("Serial"); }
        }

        private string _manu;
        public string Manufacturer
        {
            get { return _manu; }
            set { _manu = value; Changed("Manufacturer"); }
        }

        private string _devname;
        public string DeviceName
        {
            get { return _devname; }
            set { _devname = value; Changed("DeviceName"); }
        }

        public int ApplicationId;

        private string _appName;
        public string ApplicationName
        {
            get { return _appName; }
            set { _appName = value; Changed("ApplicationName"); }
        }


        public LineDevice LineDevice { get; set; }



        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
