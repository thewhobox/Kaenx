using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.Context.Catalog
{
    public class AppParameterRef
    {
        public string Id { get; set; }
        public string RefId { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }
        public AccessType Access { get; set; }
    }
}
