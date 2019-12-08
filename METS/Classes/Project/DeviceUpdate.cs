using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.Classes.Project
{
    public class DeviceUpdate
    {
        public string Name { get; set; }
        public string VersionCurrent { get; set; }
        public string VersionNew { get; set; }
        public string NewApplicationId { get; set; }
        public int DeviceUid { get; set; }
        public LineDevice Device { get; set; }
    }
}
