using Kaenx.Classes.Buildings;
using Kaenx.DataContext.Local;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Project
{
    public class Project : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] Image { get; set; }
        public LocalConnectionProject Connection { get; set; }
        public LocalProject Local { get; set; }

        private ObservableCollection<Group> _groups = new ObservableCollection<Group>();

        public event PropertyChangedEventHandler PropertyChanged;

        public Area Area { get; set; } = new Area();

        public ObservableCollection<Line> Lines { get; set; } = new ObservableCollection<Line>();
        public ObservableCollection<Group> Groups
        {
            get { return _groups; }
            set { _groups = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Groups")); }
        }

        public Project() { }

        public Project(string _name)
        {
            Name = _name;
        }
    }
}
