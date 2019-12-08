using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace METS.Context.Project
{
    public class ComObject
    {
        [Key]
        public int Id { get; set; }
        public string ComId { get; set; }
        public int DeviceId { get; set; }
        public string Groups { get; set; }
    }
}
