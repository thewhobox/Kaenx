﻿using Kaenx.Classes.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus.Data
{
    public interface IBusData
    {
        public string Type { get; }
        public LineDevice Device { get; set; }
        public string Manufacturer { get; set; }
        public string SerialNumber { get; set; }
        public string ApplicationName { get; set; }
        public string Additional { get; set; }
    }
}
