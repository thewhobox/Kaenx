using Kaenx.Classes.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Dynamic
{
    public class ParameterTable: INotifyPropertyChanged, IDynParameter
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        [JsonProperty("i")]
        public int Id { get; set; }
        [JsonProperty("a")]
        public bool HasAccess { get; set; } = true;
        [JsonProperty("t")]
        public string Text { get; set; }
        [JsonProperty("h")]
        public string Hash { get; set; }
        [JsonProperty("s")]
        public string SuffixText { get; set; }
        [JsonProperty("v")]
        public string Value { get; set; }

        [JsonProperty("d")]
        public string Default { get; set; }
        [JsonProperty("e")]
        public bool IsEnabled { get; set; }

        private Visibility _visible;
        [JsonProperty("vi")]
        public Visibility Visible
        {
            get { return _visible; }
            set { 
                _visible = value; 
                Changed("Visible"); 
            }
        }

        public List<TableColumn> Columns {get;set;} = new List<TableColumn>();
        public List<TableRow> Rows {get;set;} = new List<TableRow>();

        [JsonProperty("c")]
        public List<ParamCondition> Conditions { get; set; } = new List<ParamCondition>();
        [JsonProperty("p")]
        public List<IDynParameter> Parameters { get; set; } = new List<IDynParameter>();
        public List<TablePosition> Positions {get;set;} = new List<TablePosition>();
    }

    public class TablePosition {
        public int Row {get;set;}
        public int Column {get;set;}
    }

    public class TableColumn {
        public float Width {get;set;}
        public UnitTypes Unit {get;set;}
    }

    public class TableRow {
        public float Height {get;set;}
        public UnitTypes Unit {get;set;}
    }

    public enum UnitTypes {
        Percentage,
        Absolute
    }
}
