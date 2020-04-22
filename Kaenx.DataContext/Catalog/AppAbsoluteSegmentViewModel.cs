using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class AppSegmentViewModel
    {
        [Key]
        [MaxLength(255)]
        public string Id { get; set; }
        public string ApplicationId { get; set; }
        public int Address { get; set; }
        public int Size { get; set; }
        public int Offset { get; set; } = 0;
        public int LsmId { get; set; }
        public string Data { get; set; }
        public string Mask { get; set; }
    }
}
