﻿using Kaenx.Classes.Helper;
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
    public class LineMiddle : INotifyPropertyChanged, TopologieBase
    {
        private bool _isExpanded;
        private int _id;
        private string _name;
        private LineState _state;

        public LineState State
        {
            get { return _state; }
            set { _state = value; Changed("State"); }
        }
        public int Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); Changed("LineName"); if (Parent != null) Parent.Subs.Sort(l => l.Id); }
        }
        public int UId { get; set; }
        public Symbol Icon { get; set; } = Symbol.AllApps;
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }
        public Line Parent { get; set; }
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; Changed("IsExpanded"); }
        }
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

        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
