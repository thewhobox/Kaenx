using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Kaenx.Classes.Project
{
    public class LineDevice : INotifyPropertyChanged, TopologieBase
    {
        public event NotifyCollectionChangedEventHandler ComObjectsChanged;

        private string _name;
        private ObservableRangeCollection<DeviceComObject> _comObjects = new ObservableRangeCollection<DeviceComObject>();

        private int _id;
        private bool _loadedGroups = false;
        private bool _loadedApplication = false;
        private bool _loadedPA = false;
        private bool _isDeactivated = false;
        private int _lastGroupCount = -1;
        public bool LoadedGroup { 
            get { return _loadedGroups; } 
            set {
                if (_loadedGroups == value) return;
                _loadedGroups = value; 
                Changed("LoadedGroup");
            } 
        }
        public bool LoadedApplication { 
            get { return _loadedApplication; } 
            set { 
                if (_loadedApplication == value) return; 
                _loadedApplication = value; 
                Changed("LoadedApplication");
            } 
        }
        public bool LoadedPA { 
            get { return _loadedPA; } 
            set {
                if (_loadedPA == value) return;
                _loadedPA = value; 
                Changed("LoadedPA");
            } 
        }
        public bool IsDeactivated { 
            get { return _isDeactivated; } 
            set {
                if (_isDeactivated == value) return;
                _isDeactivated = value;
                Changed("IsDeactivated");
            }
        }
        public int LastGroupCount
        {
            get { return _lastGroupCount; }
            set
            {
                _lastGroupCount = value;
                Changed("LastGroupCount");
            }
        }
        public bool IsExpanded { get { return false; } }
        public List<string> Subs { get; }

        private byte[] _serial;
        public byte[] Serial
        {
            get { return _serial; }
            set { _serial = value; Changed("Serial"); Changed("SerialText"); }
        }
        public string SerialText { get { return Serial == null ? "" : BitConverter.ToString(Serial).Replace("-", ""); } }
        public int Id
        {
            get { return _id; }
            set
            {
                if (_id == value) return;
                _id = value;
                Changed("Id");
                Changed("LineName");
                LoadedPA = false;
                Parent?.Subs.Sort(x => x.Id);
                Parent?.Changed("Subs");
            }
        }
        //TODO speichern ändern! Nicht immer das ganze Projekt!

        public ObservableRangeCollection<DeviceComObject> ComObjects
        {
            get { return _comObjects; }
            set { _comObjects = value; Changed("ComObjects"); }
        }
        public int UId { get; set; }
        [XmlIgnore]
        public Symbol Icon { get; set; } = Symbol.MapDrive;
        public string Name
        {
            get { return _name; }
            set {
                if (_name == value) return;
                _name = value; 
                Changed("Name"); 
            }
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

        public int DeviceId { get; set; }
        public int ApplicationId { get; set; }

        public LineDevice()
        {
            ComObjects.CollectionChanged += ComObjects_CollectionChanged;
        }

        public LineDevice(DataContext.Catalog.DeviceViewModel model, LineMiddle parent)
        {
            Name = model.Name;
            Parent = parent;
            Parent.PropertyChanged += Parent_PropertyChanged;
            ComObjects.CollectionChanged += ComObjects_CollectionChanged;
        }

        private void ComObjects_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ComObjectsChanged?.Invoke(this, e);
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
            Serial = model.Serial;

            LoadedApplication = model.LoadedApp;
            LoadedGroup = model.LoadedGA;
            LoadedPA = model.LoadedPA;

            Parent.PropertyChanged += Parent_PropertyChanged;
            ComObjects.CollectionChanged += ComObjects_CollectionChanged;
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
