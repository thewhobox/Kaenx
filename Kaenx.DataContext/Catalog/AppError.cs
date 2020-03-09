using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class AppError
    {
        public string AppId { get; set; }
        public string Node { get; set; }
        public string Param { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }

        public AppError(string id, string node, string param, string type, string msg)
        {
            AppId = id;
            Node = node;
            Param = param;
            Type = type;
            Message = msg;
        }
    }
}
