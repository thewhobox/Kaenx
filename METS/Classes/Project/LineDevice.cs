using METS.Classes.Helper;
using METS.Classes.Project;
using METS.Context.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace METS.Classes
{
    public class LineDevice : INotifyPropertyChanged, TopologieBase
    {
        private int _id;
        private string _name;
        private ObservableCollection<DeviceComObject> _comObjects = new ObservableCollection<DeviceComObject>();


        private bool _loadedGroups = true;
        private bool _loadedApplication = true;
        private bool _loadedPA = true;
        public bool LoadedGroups { get { return _loadedGroups; } set { _loadedGroups = value; Changed("LoadedGroups"); } }
        public bool LoadedApplication { get { return _loadedApplication; } set { _loadedApplication = value; Changed("LoadedApplication"); } }
        public bool LoadedPA{ get { return _loadedPA; } set { _loadedPA = value; Changed("LoadedPA"); } }

        //TODO speichern ändern! Nicht immer das ganze Projekt!

        [XmlIgnore]
        public SolidColorBrush CurrentBrush { get; set; } = new SolidColorBrush(Windows.UI.Colors.Black);
        public int Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); Changed("LineName"); LoadedPA = false; Parent?.Subs.Sort(x => x.Id); SaveHelper.SaveProject(); }
        }
        public ObservableCollection<DeviceComObject> ComObjects
        {
            get { return _comObjects; }
            set { _comObjects = value; Changed("ComObjects"); SaveHelper.SaveProject(); }
        }
        public int UId { get; set; }
        [XmlIgnore]
        public Symbol Icon { get; set; } = Symbol.MapDrive;
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); SaveHelper.SaveProject(); }
        }
        [XmlIgnore]
        public LineMiddle Parent { get; set; }
        [XmlIgnore]
        public TopologieType Type { get; set; } = TopologieType.Device;
        public string LineName
        {
            get
            {
                return Parent.Parent.Id + "." + Parent.Id + "." + ((Id == -1) ? "-" : Id.ToString());
            }
        }

        public string DeviceId { get; set; }
        public string ApplicationId { get; set; }

        public LineDevice() { }
        public LineDevice(Context.Catalog.DeviceViewModel model, LineMiddle parent)
        {
            Name = model.Name;
            Parent = parent;
            Parent.PropertyChanged += Parent_PropertyChanged;
        }

        private void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "LineName")
                Changed("LineName");
        }

        public LineDevice(LineDeviceModel model, LineMiddle line)
        {
            Id = model.Id;
            UId = model.UId;
            Name = model.Name;
            Parent = line;
            ApplicationId = model.ApplicationId;
            Parent.PropertyChanged += Parent_PropertyChanged;
        }

        public void ChangedComs()
        {
            Changed("ComObjects");
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
