using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class ImportError
    {
        public string Id;
        public string Exception;
        public string Code;
        public string Message;

        public ImportError(string id) => Id = id;
    }
}
