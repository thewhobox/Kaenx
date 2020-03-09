using Kaenx.DataContext.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Kaenx.Classes.Project
{
    public class GroupMiddle : INotifyPropertyChanged
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
        public Group Parent { get; set; }
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; Changed("IsExpanded"); }
        }
        public string GroupName
        {
            get
            {
                return Parent.Id + "/" + Id;
            }
        }
        public Symbol Icon { get; } = Symbol.ViewAll;

        public ObservableCollection<GroupAddress> Subs { get; set; } = new ObservableCollection<GroupAddress>();

        public GroupMiddle() { }
        public GroupMiddle(int id, string name, Group parent)
        {
            Id = id;
            Name = name;
            Parent = parent;
        }
        public GroupMiddle(GroupMiddleModel model, Group parent)
        {
            Id = model.Id;
            UId = model.UId;
            Name = model.Name;
            Parent = parent;
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
