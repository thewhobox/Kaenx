using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class AppParameterRef
    {
        [Key]
        [MaxLength(255)]
        public string Id { get; set; }
        public string RefId { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }
        public AccessType Access { get; set; }
    }
}
