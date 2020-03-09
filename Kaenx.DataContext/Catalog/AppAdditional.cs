using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.DataContext.Catalog
{
    public class AppAdditional
    {
        public string Id { get; set; }
        public byte[] LoadProcedures { get; set; }
        public byte[] Dynamic { get; set; }
        public byte[] ParameterAll { get; set; }
        public byte[] ParameterDefault { get; set; }
        public byte[] ComsAll { get; set; }
        public byte[] ComsDefault { get; set; }
    }
}
