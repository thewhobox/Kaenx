﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Buildings
{
    public class Building : INotifyPropertyChanged, IBuildingStruct
    {
        public ObservableCollection<Floor> Floors { get; set; } = new ObservableCollection<Floor>();

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; Changed("IsExpanded"); }
        }

        [JsonIgnore]
        public Area ParentArea { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
