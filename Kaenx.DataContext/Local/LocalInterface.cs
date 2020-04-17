using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;

namespace Kaenx.DataContext.Local
{
    public class LocalInterface
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhAddr { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
    }
}
