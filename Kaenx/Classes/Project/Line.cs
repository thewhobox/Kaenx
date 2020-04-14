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

namespace Kaenx.Classes.Project
{
    public class Line : INotifyPropertyChanged, TopologieBase
    {
        public bool IsInit = false;

        private bool _isExpanded;
        private int _id;
        private string _name;

        [XmlIgnore]
        public SolidColorBrush CurrentBrush { get; set; } = new SolidColorBrush(Windows.UI.Colors.White);
        [XmlIgnore]
        public SolidColorBrush CurrentBackBrush { get; set; } = new SolidColorBrush(Windows.UI.Colors.Transparent);
        public int Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); Changed("LineName"); if(!IsInit) SaveHelper.SaveLine(this); }
        }
        public int UId { get; set; }
        [XmlIgnore]
        public Symbol Icon { get; set; } = Symbol.DockLeft;
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); if (!IsInit) SaveHelper.SaveLine(this); }
        }
        [XmlIgnore]
        public Line Parent { get; set; }
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; Changed("IsExpanded"); }
        }
        [XmlIgnore]
        public TopologieType Type { get; set; } = TopologieType.Line;
        public string LineName { get { return Id.ToString(); } }
        public ObservableCollection<LineMiddle> Subs { get; set; } = new ObservableCollection<LineMiddle>();

        public Line() { }
        public Line(int id, string name)
        {
            IsInit = true;
            Id = id;
            Name = name;
            Parent = null;
            IsInit = false;
        }
        public Line(LineModel model)
        {
            IsInit = true;
            Id = model.Id;
            UId = model.UId;
            Name = model.Name;
            IsExpanded = model.IsExpanded;
            Parent = null;
            IsInit = false;
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}


