using Kaenx.Classes.Project;
using Kaenx.Konnect.Addresses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Buildings
{
    public class FunctionGroup
    {
        public MulticastAddress Address { get; set; }
        public GroupAddress GroupAddress { get; set; }
        public string Name { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public Function ParentFunction { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public ObservableCollection<DeviceComObject> ComObjects { get; set; } = new ObservableCollection<DeviceComObject>();

        public FunctionGroup() { }

        public FunctionGroup(Function func)
        {
            ParentFunction = func;
        }
    }
}
