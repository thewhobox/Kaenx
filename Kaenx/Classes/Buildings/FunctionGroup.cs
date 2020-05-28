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
        public DataPointSubType DPST { get; set; }
        public MulticastAddress Address { get; set; }
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

        public DataPointSubType DataPointSubType
        {
            get
            {
                if(DPST != null)
                {
                    return DPST;
                }
                else
                {
                    if (ComObjects.Count == 0)
                    {
                        return new DataPointSubType() { Name = "..." };
                    }
                    else
                    {
                        if (ComObjects.Any(c => c.DataPointSubType.Name != "xxx"))
                        {
                            return ComObjects.First(c => c.DataPointSubType.Name != "xxx").DataPointSubType;
                        }
                        else
                        {
                            return ComObjects.First(c => c.DataPointSubType.Name != "xxx").DataPointSubType;
                        }
                    }
                }
            }
        }
    }
}
