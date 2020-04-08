using Kaenx.Classes.Helper;
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
    public class LineMiddle : INotifyPropertyChanged, TopologieBase
    {
        private bool _isExpanded;
        private int _id;
        private string _name;
        private SolidColorBrush _currentBrush = new SolidColorBrush(Windows.UI.Colors.White);

        [XmlIgnore]
        public SolidColorBrush CurrentBrush
        {
            get { return _currentBrush; }
            set { _currentBrush = value; Changed("CurrentBrush"); }
        }
        [XmlIgnore]
        public SolidColorBrush CurrentBackBrush { get; set; } = new SolidColorBrush(Windows.UI.Colors.Transparent);
        public int Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); Changed("LineName"); if(Parent != null) Parent.Subs.Sort(l => l.Id); SaveHelper.SaveProject(); }
        }
        public int UId { get; set; }
        [XmlIgnore]
        public Symbol Icon { get; set; } = Symbol.AllApps;
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); SaveHelper.SaveProject(); }
        }
        [XmlIgnore]
        public Line Parent { get; set; }
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; Changed("IsExpanded"); }
        }
        [XmlIgnore]
        public TopologieType Type { get; set; } = TopologieType.LineMiddle;
        public string LineName { get { return Parent.Id + "." + Id; } }
        public ObservableCollection<LineDevice> Subs { get; set; } = new ObservableCollection<LineDevice>();

        public LineMiddle() { }
        public LineMiddle(int id, string name, Line parent)
        {
            Id = id;
            Name = name;
            Parent = parent;
            Parent.PropertyChanged += Parent_PropertyChanged;
        }

        private void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LineName")
                Changed("LineName");
        }

        public LineMiddle(LineMiddleModel model, Line line)
        {
            Id = model.Id;
            UId = model.UId;
            Name = model.Name;
            IsExpanded = model.IsExpanded;
            Parent = line;
            Parent.PropertyChanged += Parent_PropertyChanged;
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
