using Kaenx.DataContext.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Kaenx.Classes.Project
{
    public class GroupAddress : INotifyPropertyChanged
    {
        private SolidColorBrush _currentBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
        private bool _isExpanded;
        private int _id;
        private string _name;

        public SolidColorBrush CurrentBrush
        {
            get { return _isExpanded ? new SolidColorBrush(Windows.UI.Colors.Transparent) : _currentBrush; }
            set { _currentBrush = value; Changed("CurrentBrush"); Changed("Icon"); }
        }

        public int UId { get; set; }

        public int Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); }
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }
        public GroupMiddle Parent { get; set; }
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; Changed("IsExpanded"); Changed("CurrentBrush"); }
        }
        public string GroupName
        {
            get
            {
                if (Id == -1) return "";
                return Parent.Parent.Id + "/" + Parent.Id + "/" + Id;
            }
        }
        public ObservableCollection<DeviceComObject> ComObjects { get; set; } = new ObservableCollection<DeviceComObject>();

        public Symbol Icon { get { return CurrentBrush.Color == Windows.UI.Colors.Transparent ? Symbol.Stop : Symbol.Import; } }

        public GroupAddress() { }
        public GroupAddress(int id, string name, GroupMiddle parent)
        {
            Id = id;
            Name = name;
            Parent = parent;
            ComObjects.CollectionChanged += ComObjects_CollectionChanged;
        }

        private void ComObjects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Changed("DPT");
            Changed("DPST");
        }

        public GroupAddress(GroupAddressModel model, GroupMiddle parent)
        {
            Id = model.Id;
            Name = model.Name;
            Parent = parent;
            UId = model.UId;
        }


        public int DPT { get { if (ComObjects.Count != 0) return ComObjects[0].Datapoint; else return 0; } }
        public int DPST { get { if (ComObjects.Count != 0) return ComObjects[0].DatapointSub; else return 0; } }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
