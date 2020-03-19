using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Project;
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

namespace Kaenx.Classes
{
    public class LineDevice : INotifyPropertyChanged, TopologieBase
    {
        public bool IsInit = false;

        private int _id;
        private string _name;
        private ObservableCollection<DeviceComObject> _comObjects = new ObservableCollection<DeviceComObject>();


        private bool _loadedGroups = false;
        private bool _loadedApplication = false;
        private bool _loadedPA = false;
        private bool _isDeactivated = false;
        public bool LoadedGroup { get { return _loadedGroups; } set { _loadedGroups = value; Changed("LoadedGroups"); if(!IsInit) SaveHelper.UpdateDevice(this); } }
        public bool LoadedApplication { get { return _loadedApplication; } set { _loadedApplication = value; Changed("LoadedApplication"); if (!IsInit) SaveHelper.UpdateDevice(this); } }
        public bool LoadedPA { get { return _loadedPA; } set { _loadedPA = value; Changed("LoadedPA"); if (!IsInit) SaveHelper.UpdateDevice(this); } }
        public bool IsDeactivated { get { return _isDeactivated; } set { _isDeactivated = value; Changed("CurrentBackBrush"); if (!IsInit) SaveHelper.UpdateDevice(this); } }
        public bool IsExpanded { get { return false; } }
        public List<string> Subs { get; }

        //TODO speichern ändern! Nicht immer das ganze Projekt!

        [XmlIgnore]
        public SolidColorBrush CurrentBrush { get; set; } = new SolidColorBrush(Windows.UI.Colors.Black);
        [XmlIgnore]
        public SolidColorBrush CurrentBackBrush { get { if (_isDeactivated) return new SolidColorBrush(Windows.UI.Colors.LightYellow); else return new SolidColorBrush(Windows.UI.Colors.Transparent); } }
        public int Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); Changed("LineName"); LoadedPA = false; Parent?.Subs.Sort(x => x.Id); if (!IsInit) SaveHelper.UpdateDevice(this); }
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
            set { _name = value; Changed("Name"); if (!IsInit) SaveHelper.UpdateDevice(this); }
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

        public LineDevice(bool isInit = false) => IsInit = isInit;

        public LineDevice(DataContext.Catalog.DeviceViewModel model, LineMiddle parent, bool isInit = false)
        {
            IsInit = isInit;
            Name = model.Name;
            Parent = parent;
            Parent.PropertyChanged += Parent_PropertyChanged;
        }

        private void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "LineName")
                Changed("LineName");
        }

        public LineDevice(LineDeviceModel model, LineMiddle line, bool isInit = false)
        {
            IsInit = isInit;
            Id = model.Id;
            UId = model.UId;
            Name = model.Name;
            Parent = line;
            ApplicationId = model.ApplicationId;

            LoadedApplication = model.LoadedApp;
            LoadedGroup = model.LoadedGA;
            LoadedPA = model.LoadedPA;

            Parent.PropertyChanged += Parent_PropertyChanged;
        }

        public void ChangedComs()
        {
            Changed("ComObjects");
        }

        private void Changed(string name)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            } catch
            {
                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                });
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
