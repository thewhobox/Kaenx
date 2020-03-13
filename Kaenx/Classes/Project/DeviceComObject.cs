using Kaenx.Classes.Controls.Paras;
using Kaenx.DataContext.Catalog;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Project
{
    public class DeviceComObject : INotifyPropertyChanged
    {
        public DeviceComObject() 
        {
            Groups.CollectionChanged += Groups_CollectionChanged;
        }

        public DeviceComObject(AppComObject comObj)
        {
            Id = comObj.Id;
            Number = comObj.Number;
            Name = comObj.Text;
            Function = comObj.FunctionText;

            Datapoint = comObj.Datapoint;
            DatapointSub = comObj.DatapointSub;
            Groups.CollectionChanged += Groups_CollectionChanged;

            Flag_Read = comObj.Flag_Read;
            Flag_Write = comObj.Flag_Write;
            Flag_Update = comObj.Flag_Update;
            Flag_Transmit = comObj.Flag_Transmit;
            Flag_Communication = comObj.Flag_Communicate;
            Flag_ReadOnInit = comObj.Flag_ReadOnInit;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Groups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Connections"));
        }

        private string _name;

        public string Id { get; set; }
        public int Datapoint { get; set; }
        public int DatapointSub { get; set; }
        public int Number { get; set; }
        public string Name { get { return _name; } set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public string Function { get; set; }
        public List<ParamCondition> Conditions { get; set; }
        [JsonIgnore]
        public ObservableCollection<GroupAddress> Groups { get; set; } = new ObservableCollection<GroupAddress>();
        [JsonIgnore]
        public string Connections
        {
            get
            {
                string groups = "";
                foreach(GroupAddress group in Groups)
                {
                    groups += ", " + group.GroupName;
                }
                return groups == "" ? "" : groups.Substring(2);
            }
        }
        public Visibility Visible { get; set; }

        public bool Flag_Read { get; set; }
        public bool Flag_Write { get; set; }
        public bool Flag_Update { get; set; }
        public bool Flag_Transmit { get; set; }
        public bool Flag_Communication { get; set; }
        public bool Flag_ReadOnInit { get; set; }
    }
}
