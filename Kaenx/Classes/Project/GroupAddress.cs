using Kaenx.DataContext.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Kaenx.Classes.Project
{
    public class GroupAddress : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private int _id;
        private string _name;

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
            set { _isExpanded = value; Changed("IsExpanded"); }
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

        public Symbol Icon { get; } = Symbol.Stop;

        public GroupAddress() { }
        public GroupAddress(int id, string name, GroupMiddle parent)
        {
            Id = id;
            Name = name;
            Parent = parent;
        }
        public GroupAddress(GroupAddressModel model, GroupMiddle parent)
        {
            Id = model.Id;
            Name = model.Name;
            Parent = parent;
            UId = model.UId;
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
