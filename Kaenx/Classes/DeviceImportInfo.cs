using Kaenx.DataContext.Catalog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Kaenx.Classes
{
    public class DeviceImportInfo : INotifyPropertyChanged
    {
        //TODO move this to view!
        private Symbol _icon = Symbol.More;
        public Symbol Icon
        {
            get { return _icon; }
            set { _icon = value; 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Icon")); 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IconBrush")); }
        }

        public SolidColorBrush IconBrush
        {
            get
            {
                switch (Icon)
                {
                    case Symbol.Like:
                        return new SolidColorBrush(Windows.UI.Colors.Green);
                    case Symbol.ReportHacked:
                        return new SolidColorBrush(Windows.UI.Colors.Red);
                    default:
                        return new SolidColorBrush(Windows.UI.Colors.Black);
                }
            }
        }


        public string Id { get; set; }
        public string Name { get; set; }
        public int ManuId { get; set; }
        public string Description { get; set; }
        public Reference ApplicationRef { get; set; }
        public Reference HardwareRef { get; set; }
        public string ProductRef { get; set; }
        public int CatalogId { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
