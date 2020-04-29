﻿using Kaenx.DataContext.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Helper
{
    public class ParamVisHelper
    {
        public string ParameterId { get; set; }
        public string Hash { get; set; }
        public List<Dynamic.ParamCondition> Conditions { get; set; }

        public ParamVisHelper() { }

        public ParamVisHelper(string paraId)
        {
            ParameterId = paraId;
        }
    }
}
