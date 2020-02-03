using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace METS.Classes
{
    public class DeviceImportInfo : INotifyPropertyChanged
    {
        private Symbol _icon = Symbol.More;
        public Symbol Icon
        {
            get { return _icon; }
            set { _icon = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Icon")); }
        }


        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string HardwareRefId { get; set; }
        public string ProductRefId { get; set; }
        public string CatalogId { get; set; }

        public string ApplicationId { get; set; }
        public string HardwareId { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
