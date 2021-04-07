using Kaenx.Classes.Buildings;
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
using Windows.UI.Xaml.Media;

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
            BindedId = comObj.BindedId;
            Number = comObj.Number;
            Name = comObj.Text;
            Function = comObj.FunctionText;
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

        [JsonProperty("d")]
        public DataPointSubType DataPointSubType { get; set; }
        [JsonProperty("is")]
        public bool IsSelected { get; set; } = false;
        [JsonProperty("ie")]
        public bool IsEnabled { get; set; } = true;
        [JsonProperty("io")]
        public bool IsOk { get; set; } = true;

        private string _name;
        private string _dname;

        [JsonProperty("i")]
        public string Id { get; set; }
        [JsonProperty("pd")]
        public LineDevice ParentDevice { get; set; }
        [JsonProperty("bi")]
        public string BindedId { get; set; }
        [JsonProperty("nu")]
        public int Number { get; set; }
        [JsonProperty("na")]
        public string Name { get { return _name; } set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        [JsonProperty("dn")]
        public string DisplayName { get { return _dname; } set { _dname = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DisplayName")); } }
        [JsonProperty("f")]
        public string Function { get; set; }
        [JsonProperty("c")]
        public List<Dynamic.ParamCondition> Conditions { get; set; }
        [JsonIgnore]
        public ObservableCollection<FunctionGroup> Groups { get; set; } = new ObservableCollection<FunctionGroup>();
        [JsonIgnore]
        public string Connections
        {
            get
            {
                string groups = "";
                foreach(FunctionGroup group in Groups)
                {
                    if(group != null)
                        groups += ", " + group.Address.ToString();
                }
                return groups == "" ? "" : groups.Substring(2);
            }
        }
        [JsonProperty("v")]
        public Visibility Visible { get; set; }


        [JsonProperty("fr")]
        public bool Flag_Read { get; set; }
        [JsonProperty("fw")]
        public bool Flag_Write { get; set; }
        [JsonProperty("fu")]
        public bool Flag_Update { get; set; }
        [JsonProperty("ft")]
        public bool Flag_Transmit { get; set; }
        [JsonProperty("fc")]
        public bool Flag_Communication { get; set; }
        [JsonProperty("fi")]
        public bool Flag_ReadOnInit { get; set; }
    }
}
