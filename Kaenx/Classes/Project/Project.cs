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

        public event PropertyChangedEventHandler PropertyChanged;

        public Area Area { get; set; } = new Area();

        public ObservableCollection<Line> Lines { get; set; } = new ObservableCollection<Line>();

        public Project() { }

        public Project(string _name)
        {
            Name = _name;
        }
    }
}
