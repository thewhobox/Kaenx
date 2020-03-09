﻿using Kaenx.DataContext.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.UI.Xaml.Controls;

namespace Kaenx.Classes.Project
{
    public class Group : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private int _id;
        private string _name;

        public int UId { get; set; }

        public Symbol Icon { get; } = Symbol.SelectAll;

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
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; Changed("IsExpanded"); }
        }
        public string GroupName
        {
            get
            {
                return Id.ToString();
            }
        }
        public ObservableCollection<GroupMiddle> Subs { get; set; } = new ObservableCollection<GroupMiddle>();

        public Group() { }
        public Group(int id, string name)
        {
            Id = id;
            Name = name;
        }
        public Group(GroupMainModel model)
        {
            Id = model.Id;
            UId = model.UId;
            Name = model.Name;
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}